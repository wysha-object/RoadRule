import CheckSvg from 'assets/images/check.svg'
import EditSvg from 'assets/images/edit.svg'
import { ChangeEvent, KeyboardEvent, useEffect, useState } from 'react'
import { Button } from 'cs2/ui'

export default function TextInput(props: {
  style?: React.CSSProperties
  value: string
  displayWhenEmpty?: string
  onChange: (value: string) => void
}) {
  const [textFieldActive, setTextFieldActive] = useState(false)
  const [textFieldValue, setTextFieldValue] = useState('')
  const enableTextField = () => {
    setTextFieldActive(true)
  }
  const submitTextField = () => {
    setTextFieldActive(false)
    props.onChange(textFieldValue)
  }
  const textFieldChangeHandler = (event: ChangeEvent<HTMLInputElement>) => {
    setTextFieldValue(event.target.value)
  }
  const textFieldKeyDownHandler = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key == 'Enter') {
      submitTextField()
    }
  }
  useEffect(() => {
    setTextFieldValue(props.value)
  }, [props.value])
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        ...props.style,
      }}
    >
      <div style={{ flexGrow: 1, flexBasis: 0 }}>
        {textFieldActive ? (
          <input
            style={{ 
              width: '100%',
              backgroundColor: 'rgba(28, 37, 52, 0.72)',
              borderTopColor: 'rgba(255, 255, 255, 0.5)',
              borderLeftColor: 'rgba(255, 255, 255, 0.5)',
              borderRightColor: 'rgba(255, 255, 255, 0.5)',
              borderBottomColor: 'rgba(255, 255, 255, 0.5)',
              borderTopLeftRadius: '3rem 3rem',
              borderTopRightRadius: '3rem 3rem',
              borderBottomLeftRadius: '3rem 3rem',
              borderBottomRightRadius: '3rem 3rem',
              textAlign: 'center',
              lineHeight: '1em',
              color: 'var(--textColor)',
            }}
            type='text'
            onChange={textFieldChangeHandler}
            onKeyDown={textFieldKeyDownHandler}
            value={textFieldValue}
            autoFocus
          />
        ) : (
          <>
            {textFieldValue || (
              <div style={{ opacity: 0.5 }}>{props.displayWhenEmpty}</div>
            )}
          </>
        )}
      </div>
      <div className='vertical-gap' />
      {textFieldActive ? (
        <Button variant='round' onClick={submitTextField}>
          <CheckSvg />
        </Button>
      ) : (
        <Button variant='round' onClick={enableTextField}>
          <EditSvg />
        </Button>
      )}
    </div>
  )
}
