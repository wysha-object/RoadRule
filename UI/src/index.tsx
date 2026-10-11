import 'assets/styles/index.scss'
import LogoIcon from 'components/icon/logo-icon'
import { RulesClipboardContext, UIToolModeContext } from 'context'
import { ModRegistrar } from 'cs2/modding'
import { Button, Tooltip } from 'cs2/ui'
import {
  clearSelectedLaneIndex,
  setToolStateCmd,
  useGetLanesCmd,
  useGetToolStateCmd,
} from 'hooks/cmd'
import { useTranslate } from 'hooks/translate'
import LanePanel from 'pages/lane-panel'
import MainPanel from 'pages/main-panel'
import { useCallback, useEffect, useRef, useState } from 'react'
import { LaneRulesValue, ToolState, UIToolMode } from 'types'

const register: ModRegistrar = (moduleRegistry) => {
  moduleRegistry.append('GameTopLeft', () => <App />)
}

function App() {
  const { t } = useTranslate()

  const toolState = useGetToolStateCmd()
  const [mode, setMode] = useState<UIToolMode>(UIToolMode.MasterLane)
  const [laneRulesValue, setLaneRulesValue] = useState<LaneRulesValue | null>(null)

  const floatingButtonClickHandler = useCallback(() => {
    if (toolState !== ToolState.Disabled) {
      setToolStateCmd(ToolState.Disabled)
    } else {
      setToolStateCmd(ToolState.Choosing)
    }
  }, [toolState])

  useEffect(() => {
    clearSelectedLaneIndex()
  }, [mode])

  return (
    <div id='road-rule-root'>
      <UIToolModeContext.Provider value={[mode, setMode]}>
        <RulesClipboardContext.Provider value={{ value: laneRulesValue, setClipboard: setLaneRulesValue }}>
          <Tooltip tooltip={t('RoadRule')}>
            <Button variant='floating' onSelect={floatingButtonClickHandler}>
              <LogoIcon />
            </Button>
          </Tooltip>
          <MainPanel />
          <LanePanel />
        </RulesClipboardContext.Provider>
      </UIToolModeContext.Provider>
    </div>
  )
}

export default register
