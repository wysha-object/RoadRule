import CheckSvg from 'assets/images/check.svg'
import EditSvg from 'assets/images/edit.svg'
import ResetSettingsSvg from 'assets/images/reset-settings.svg'
import { ChangeEvent, KeyboardEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useTranslate } from 'hooks/translate'
import TipArea from 'components/base/tip-area'
import { Button } from 'cs2/ui'
import TipIcon from 'components/icon/tip-icon'
import styled from 'styled-components'

const RangeComponent = styled.div`
  padding: 0.25em 0;
  width: 100%;
`

const Track = styled.div`
  background-color: rgba(255, 255, 255, 0.5);
  border-radius: 0.25em;
  width: 100%;
  height: 0.5em;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  flex-direction: row;
  padding: 0 0.5em;
`

const Filler = styled.div`
  background-color: var(--accentColorNormal);
  box-shadow: var(--accentColorNormal) -0.5em 0;
  border-radius: 0.25em 0 0 0.25em;
  height: 0.5em;
`

const Thumb = styled.div<{ active: boolean }>`
  background-color: var(--textColor);
  border-radius: 50%;
  width: 1em;
  height: 1em;
  margin-left: -0.5em;
  transform: ${(props) => (props.active ? 'scale3d(1.1, 1.1, 1)' : 'none')};
  &:hover {
    transform: scale3d(1.1, 1.1, 1);
  }
`

function Range(props: {
  min: number
  max: number
  step: number
  value: number
  onChange?: (value: number) => void
  onUpdate?: (value: number) => void
}) {
  const [dragging, setDragging] = useState(false)
  const [value, setValue] = useState(0)
  const sliderRef = useRef<HTMLDivElement>(null)

  const getNewValue = useCallback(
    (clientX: number) => {
      let sliderLeft = 0
      let sliderWidth = 0
      if (sliderRef.current) {
        const rect = sliderRef.current.getBoundingClientRect()
        sliderLeft = rect.left
        sliderWidth = rect.right - rect.left
      }
      let newValue =
        Math.round(
          (((clientX - sliderLeft) / sliderWidth) * (props.max - props.min)) /
            props.step,
        ) *
          props.step +
        props.min
      if (newValue < props.min) {
        newValue = props.min
      }
      if (newValue > props.max) {
        newValue = props.max
      }
      return newValue
    },
    [props.min, props.max, props.step],
  )

  const mouseDownHandler = (_event: React.MouseEvent<HTMLElement>) => {
    setDragging(true)
  }
  const mouseUpHandler = useCallback(
    (event: MouseEvent) => {
      const newValue = getNewValue(event.clientX)
      setValue(newValue)
      setDragging(false)
      if (props.onChange) {
        props.onChange(newValue)
      }
    },
    [props, getNewValue],
  )
  const mouseMoveHandler = useCallback(
    (event: MouseEvent) => {
      const newValue = getNewValue(event.clientX)
      setValue(newValue)
      if (props.onUpdate) {
        props.onUpdate(newValue)
      }
    },
    [props, getNewValue],
  )

  useEffect(() => {
    if (dragging) {
      document.body.addEventListener('mouseup', mouseUpHandler)
      document.body.addEventListener('mousemove', mouseMoveHandler)
      return () => {
        document.body.removeEventListener('mouseup', mouseUpHandler)
        document.body.removeEventListener('mousemove', mouseMoveHandler)
      }
    }
  }, [dragging, mouseMoveHandler, mouseUpHandler])

  useEffect(() => {
    if (!dragging) {
      if (props.value < props.min || isNaN(props.value)) {
        setValue(props.min)
      } else if (props.value > props.max) {
        setValue(props.max)
      } else {
        setValue(props.value)
      }
    }
  }, [props.value, props.min, props.max, dragging])

  const sliderValue = ((value - props.min) / (props.max - props.min)) * 100

  return (
    <RangeComponent onMouseDown={mouseDownHandler}>
      <Track ref={sliderRef}>
        <Filler style={{ width: sliderValue + '%' }} />
        <Thumb active={dragging} />
      </Track>
    </RangeComponent>
  )
}


export default function RangeRow(props: {
  onChange: (value: number) => void
  label: string
  value: number
  valuePrefix: string
  valueSuffix: string
  defaultValue: number
  enableTextField?: boolean
  textFieldRegExp?: string
  min: number
  max: number
  step: number
  tooltip?: React.ReactNode
}) {
  const { t } = useTranslate()
  const [value, setValue] = useState(0)
  const [textFieldActive, setTextFieldActive] = useState(false)
  const [textFieldValue, setTextFieldValue] = useState('')
  const textFieldRegExp = useMemo(() => {
    return props.textFieldRegExp ? new RegExp(props.textFieldRegExp) : null
  }, [props.textFieldRegExp])
  const updateHandler = (value: number) => {
    setValue(value)
  }
  const enableTextField = () => {
    setTextFieldValue('')
    setTextFieldActive(true)
  }
  const submitTextField = () => {
    setTextFieldActive(false)
    if (textFieldValue.length > 0) {
      const newValue = parseFloat(textFieldValue)
      if (!isNaN(newValue)) {
        props.onChange(newValue)
      }
    }
  }
  const textFieldChangeHandler = (event: ChangeEvent<HTMLInputElement>) => {
    if (textFieldRegExp !== null) {
      if (event.target.value.match(textFieldRegExp)) {
        setTextFieldValue(event.target.value)
      }
    } else {
      setTextFieldValue(event.target.value)
    }
  }
  const textFieldKeyDownHandler = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key == 'Enter') {
      submitTextField()
    }
  }
  const resetHandler = () => {
    setTextFieldActive(false)
    props.onChange(props.defaultValue)
  }
  useEffect(() => {
    setValue(props.value)
  }, [props.value])
  return (
    <div className='row-with-hover-effect' style={{ flexDirection: 'column' }}>
      <div
        style={{
          width: '100%',
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
        }}
      >
        <div style={{ flex: '1', display: 'flex' }}>
          <div style={{ flex: '1' }}>{props.label}</div>
          {!textFieldActive
            ? t(props.valuePrefix) +
              `${Math.round(value * 100) / 100}` +
              t(props.valueSuffix)
            : ''}
        </div>
        {textFieldActive && (
          <input
            type='number'
            style={{ minWidth: '3em', width: '3em' }}
            onChange={textFieldChangeHandler}
            onKeyDown={textFieldKeyDownHandler}
            value={textFieldValue}
            autoFocus
          />
        )}
        <div className='vertical-gap' />
        {props.enableTextField && (
          <>
            {textFieldActive ? (
              <Button variant='round' onClick={submitTextField}>
                <CheckSvg />
              </Button>
            ) : (
              <Button variant='round' onClick={enableTextField}>
                <EditSvg />
              </Button>
            )}
          </>
        )}
        <div className='vertical-gap' />
        <Button variant='round' onClick={resetHandler}>
          <ResetSettingsSvg />
        </Button>
        {props.tooltip && (
          <>
            <div className='vertical-gap' />
            <TipArea position='right-start' tooltip={props.tooltip}>
              <TipIcon />
            </TipArea>
          </>
        )}
      </div>
      <div className='horizontal-gap' />
      <Range
        min={props.min}
        max={props.max}
        step={props.step}
        value={props.value}
        onChange={props.onChange}
        onUpdate={updateHandler}
      />
    </div>
  )
}
