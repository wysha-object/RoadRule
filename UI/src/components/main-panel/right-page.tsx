import {
  updateLane,
  useGetLanesCmd,
  useGetSelectedEdgeEntityCmd,
  useGetSelectedLaneIndexCmd,
} from 'hooks/cmd'
import { useContext, useState } from 'react'
import LaneList from './mods/lane-list'
import { useTranslate } from 'hooks/translate'
import { mergeLaneRules } from 'utils'
import { LaneRulesValue, UIToolMode } from 'types'
import RulesEditor from './mods/rules-editor'
import BasePage from 'components/base/base-page'
import { Button } from 'cs2/ui'
import { UIToolModeContext } from 'context'

export default function RightPage() {
  const { t } = useTranslate()
  const slectedEdgeEntities = useGetSelectedEdgeEntityCmd()

  return (
    <BasePage
      style={{
        left: 'calc(10em + 10rem)',
      }}
      header={<Header />}
    >
      <div
        style={{
          width: '20em',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <LaneList />
        <RoadRuleEditor />
      </div>
    </BasePage>
  )
}

function Header() {
  const { t } = useTranslate()
  const [mode, setMode] = useContext(UIToolModeContext)
  return (
    <div
      style={{
        height: '100%',
        display: 'flex',
        alignItems: 'flex-end',
        padding: '0 0.5em',
      }}
    >
      <Button
        style={{
          flex: '1 0 0',
          margin: '0 0.3em',
        }}
        variant='flat'
        className='top-option-button'
        onClick={() => setMode(UIToolMode.MasterLane)}
      >
        {t('MainPanel.MasterLane')}
      </Button>
      <Button
        style={{
          flex: '1 0 0',
          margin: '0 0.3em',
        }}
        variant='flat'
        className='top-option-button'
        onClick={() => setMode(UIToolMode.Lane)}
      >
        {t('MainPanel.Lane')}
      </Button>
    </div>
  )
}

function RoadRuleEditor() {
  const { t } = useTranslate()
  const [mode] = useContext(UIToolModeContext)

  const masterMap = useGetLanesCmd()
  const selectedLaneIndex = useGetSelectedLaneIndexCmd()
  let laneRulesValue: LaneRulesValue | undefined = undefined
  if (mode === UIToolMode.Lane) {
    for (const lane of Object.values(masterMap)
      .map((item) => item.lanes)
      .flat()) {
      if (!selectedLaneIndex.includes(lane.laneIndex)) {
        continue
      }

      if (laneRulesValue === undefined) {
        laneRulesValue = lane.laneRules
      } else {
        laneRulesValue = mergeLaneRules(laneRulesValue, lane.laneRules)
      }
    }
  } else {
    for (const lane of Object.values(masterMap).map(
      (item) => item.masterLane,
    )) {
      if (!selectedLaneIndex.includes(lane.laneIndex)) {
        continue
      }

      if (laneRulesValue === undefined) {
        laneRulesValue = lane.laneRules
      } else {
        laneRulesValue = mergeLaneRules(laneRulesValue, lane.laneRules)
      }
    }
  }

  return laneRulesValue === undefined ? (
    <div
      style={{
        padding: '2em',
        display: 'flex',
        justifyContent: 'center',
      }}
    >
      {mode === UIToolMode.Lane
        ? t('MainPanel.ChooseLane')
        : t('MainPanel.ChooseMasterLane')}
    </div>
  ) : (
    <RulesEditor
      laneRulesValue={laneRulesValue}
      onChange={(_, newValue) => {
        for (const laneIndex of selectedLaneIndex) {
          updateLane({
            laneIndex: laneIndex,
            key: 'lane-rules',
            value: newValue,
          })
        }
      }}
    />
  )
}
