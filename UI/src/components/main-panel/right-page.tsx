import {
  updateLane,
  useGetLanesCmd,
  useGetSelectedLaneIndexCmd,
} from 'hooks/cmd'
import { useContext } from 'react'
import LaneList from './mods/lane-list'
import { useTranslate } from 'hooks/translate'
import { mergeCarLaneValues, mergeLaneRules } from 'utils'
import { CarLaneValue, LaneRulesValue, UIToolMode } from 'types'
import RulesEditor from './mods/lane-rules-editor'
import BasePage from 'components/base/base-page'
import { Button, Scrollable } from 'cs2/ui'
import { UIToolModeContext } from 'context'
import CarLaneEditor from './mods/car-lane-editor'

export default function RightPage() {
  const { t } = useTranslate()
  const [mode] = useContext(UIToolModeContext)

  const masterMap = useGetLanesCmd()
  const selectedLaneIndex = useGetSelectedLaneIndexCmd()
  let laneRulesValue: LaneRulesValue | undefined = undefined
  let carLaneValue: CarLaneValue | undefined = undefined
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
    for (const lane of Object.values(masterMap)
      .map((item) => [...item.lanes, item.masterLane])
      .flat()) {
      if (!selectedLaneIndex.includes(lane.laneIndex)) {
        continue
      }

      if (carLaneValue === undefined) {
        carLaneValue = lane.carLane
      } else {
        carLaneValue = mergeCarLaneValues(carLaneValue, lane.carLane)
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

  return (
    <BasePage
      style={{
        left: 'calc(10em + 10rem)',
      }}
      header={<Header />}
      footer={<></>}
    >
      <LaneList />
      <div style={{ flex: '1 1 0' }}>
        <Scrollable>
          <div
            style={{
              width: '30em'
            }}
          >
            {selectedLaneIndex.length == 0 ? (
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
              <>
                {carLaneValue !== undefined &&
                  <CarLaneEditor
                    lanePropertiesValue={carLaneValue}
                    onValueChange={function (oldValue: CarLaneValue, newValue: CarLaneValue): void {
                      for (const laneIndex of selectedLaneIndex) {
                        updateLane({
                          laneIndex: laneIndex,
                          key: 'car-lane',
                          value: newValue,
                        })
                      }
                    }}
                  />
                }
                {laneRulesValue !== undefined &&
                  <RulesEditor
                    laneRulesValue={laneRulesValue}
                    onValueChange={(_, newValue) => {
                      for (const laneIndex of selectedLaneIndex) {
                        updateLane({
                          laneIndex: laneIndex,
                          key: 'lane-rules',
                          value: newValue,
                        })
                      }
                    }}
                  />
                }
              </>
            )}
          </div>
        </Scrollable>
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
        width: '30em',
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
          borderStyle: mode === UIToolMode.MasterLane ? 'solid' : 'none',
          borderWidth: '1rem 2rem 0',
          borderColor: 'white',
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
          borderStyle: mode === UIToolMode.Lane ? 'solid' : 'none',
          borderWidth: '1rem 2rem 0',
          borderColor: 'white',
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
