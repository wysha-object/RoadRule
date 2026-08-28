import { useEffect, useState } from 'react'
import { useTranslate } from 'hooks/translate'
import { Button, Scrollable, Tooltip } from 'cs2/ui'
import { ToolState } from 'types'
import BasePage from 'components/base/base-page'
import {
  useGetToolStateCmd,
  setToolStateCmd,
  useGetSelectedEdgeEntityCmd,
  lookAt,
} from 'hooks/cmd'
import TitleHeader from 'components/main-panel/mods/title-header'

export default function TooltipPage() {
  const { t } = useTranslate()

  const [showPanel, setShowPanel] = useState(false)

  const toolState = useGetToolStateCmd()

  useEffect(() => {
    setShowPanel(toolState !== ToolState.Disabled)
  }, [toolState])

  return (
    <BasePage
      style={{
        display: showPanel ? 'block' : 'none',
      }}
      header={<TitleHeader />}
    >
      <Body />
    </BasePage>
  )
}

export function Body() {
  const { t } = useTranslate()
  const toolState = useGetToolStateCmd()

  return (
    <div
      style={{
        flex: '1',
      }}
    >
      {toolState === ToolState.Choosing && (
        <div
          className='row'
          style={{ padding: '2em', justifyContent: 'center', width: '30em' }}
        >
          {t('MainPanel.ChooseEdge')}
        </div>
      )}
    </div>
  )
}
