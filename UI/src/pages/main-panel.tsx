import { useEffect, useState } from 'react'
import { useTranslate } from '../hooks/translate'
import { ToolState } from 'types'
import { useGetToolStateCmd } from 'hooks/cmd'
import RightPage from 'components/main-panel/right-page'
import LeftPage from 'components/main-panel/left-page'
import TooltipPage from 'components/main-panel/tooltip-page'

export default function MainPanel() {
  const { t } = useTranslate();

  const toolState = useGetToolStateCmd()

  return (
    <>
      {toolState === ToolState.Choosing && (
        <TooltipPage />
      )}
      {[ToolState.Choosed].includes(toolState) && (
        <>
          <LeftPage />
          <RightPage />
        </>
      )}
    </>
  )
}