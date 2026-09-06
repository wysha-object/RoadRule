import { createContext } from 'react'
import { LaneRulesValue, UIToolMode } from 'types'

const UIToolModeContext = createContext<
  [UIToolMode, (mode: UIToolMode) => void]
>([UIToolMode.Lane, () => {}])

const RulesClipboardContext = createContext<{
  value: LaneRulesValue | null
  setClipboard: (value: LaneRulesValue | null) => void
}>({
  value: null,
  setClipboard: () => {},
})

export { UIToolModeContext, RulesClipboardContext }
