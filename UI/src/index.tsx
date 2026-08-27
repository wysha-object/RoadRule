import 'assets/styles/index.scss'
import LogoIcon from 'components/icon/logo-icon'
import { UIToolModeContext } from 'context'
import { ModRegistrar } from 'cs2/modding'
import { Button, Tooltip } from 'cs2/ui'
import { clearSelectedLaneIndex, setToolStateCmd, useGetToolStateCmd } from 'hooks/cmd'
import { useTranslate } from 'hooks/translate'
import MainPanel from 'pages/main-panel'
import { useCallback, useEffect, useState } from 'react'
import { ToolState, UIToolMode } from 'types'

const register: ModRegistrar = (moduleRegistry) => {
  moduleRegistry.append('GameTopLeft', () => <App />)
}

function App() {
  const { t } = useTranslate();

  const toolState = useGetToolStateCmd()
  const [mode, setMode] = useState<UIToolMode>(UIToolMode.Lane)

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
    <div id="road-rule-root">
      <UIToolModeContext.Provider value={[mode, setMode]}>
        <Tooltip tooltip={t('RoadRule')}>
          <Button variant='floating' onSelect={floatingButtonClickHandler}>
            <LogoIcon />
          </Button>
        </Tooltip>
        <MainPanel />
      </UIToolModeContext.Provider>
    </div>
  )
}

export default register
