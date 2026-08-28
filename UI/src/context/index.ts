import { createContext } from 'react'
import { UIToolMode } from 'types'

const UIToolModeContext = createContext<
  [UIToolMode, (mode: UIToolMode) => void]
>([UIToolMode.Lane, () => {}])

export { UIToolModeContext }
