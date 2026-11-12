import CheckSvg from 'assets/images/check.svg'
import EditSvg from 'assets/images/edit.svg'
import ResetSettingsSvg from 'assets/images/reset-settings.svg'
import { ChangeEvent, KeyboardEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useTranslate } from 'hooks/translate'
import TipArea from 'components/base/tip-area'
import { Button } from 'cs2/ui'
import TipIcon from 'components/icon/tip-icon'
import styled from 'styled-components'

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
  disabled?: boolean
  range: Range
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
      let newValue = 0
      switch (props.range.type) {
        case 'step':
          newValue =
            Math.round(
              (((clientX - sliderLeft) / sliderWidth) * (props.range.max - props.range.min)) /
              props.range.step,
            ) *
            props.range.step +
            props.range.min
          if (newValue < props.range.min) {
            newValue = props.range.min
          }
          if (newValue > props.range.max) {
            newValue = props.range.max
          }
          break
        case 'list':
          let list = props.range.list.sort((a, b) => a - b)
          newValue =
            ((clientX - sliderLeft) / sliderWidth) * (list[list.length - 1] - list[0]) +
            list[0]
          newValue = list.reduce((prev, curr) => Math.abs(curr - newValue) < Math.abs(prev - newValue) ? curr : prev)
          break
      }
      return newValue
    },
    [JSON.stringify(props.range)],
  )

  const mouseDownHandler = (_event: React.MouseEvent<HTMLElement>) => {
    if (props.disabled) {
      return
    }
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
      let min
      let max
      switch (props.range.type) {
        case 'step':
          min = props.range.min
          max = props.range.max
          break
        case 'list':
          min = Math.min(...props.range.list)
          max = Math.max(...props.range.list)
          break
      }
      if (props.value < min || isNaN(props.value)) {
        setValue(min)
      } else if (props.value > max) {
        setValue(max)
      } else {
        setValue(props.value)
      }
    }
  }, [props.value, JSON.stringify(props.range), dragging])

  let min
  let max
  switch (props.range.type) {
    case 'step':
      min = props.range.min
      max = props.range.max
      break
    case 'list':
      min = Math.min(...props.range.list)
      max = Math.max(...props.range.list)
      break
  }
  const sliderValue = ((value - min) / (max - min)) * 100

  return (
    <div
      onMouseDown={mouseDownHandler}
      style={{
        padding: '0.25em 0',
        width: '100%'
      }}
    >
      <div
        ref={sliderRef}
        style={{
          backgroundColor: 'rgba(255, 255, 255, 0.5)',
          borderRadius: '0.25em',
          width: '100%',
          height: '0.5em',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'flex-start',
          flexDirection: 'row',
          padding: '0 0.5em',
        }}
      >
        <div
          style={{
            width: sliderValue + '%',
            backgroundColor: 'var(--accentColorNormal)',
            boxShadow: 'var(--accentColorNormal) -0.5em 0',
            borderRadius: '0.25em 0 0 0.25em',
            height: '0.5em',
          }}
        />
        <Thumb active={dragging} />
      </div>
    </div>
  )
}

export type Range = {
  type: 'step'
  min: number
  max: number
  step: number
} | {
  type: 'list'
  list: number[]
}

export default function RangeRow(props: {
  disabled?: boolean
  singleLine?: boolean
  onChange: (value: number) => void
  label: string
  value: number
  valuePrefix: string
  valueSuffix: string
  defaultValue: number
  enableTextField?: boolean
  textFieldRegExp?: string
  range: Range
  tooltip?: React.ReactNode
}) {
  const { t } = useTranslate()
  const [value, setValue] = useState(0)
  const [textFieldActive, setTextFieldActive] = useState(false)
  const [textFieldValue, setTextFieldValue] = useState('')
  const textFieldRegExp = useMemo(() => {
    return props.textFieldRegExp ? new RegExp(props.textFieldRegExp) : null
  }, [props.textFieldRegExp])
  const draggingRef = useRef(false)
  const updateHandler = (value: number) => {
    draggingRef.current = true
    setValue(value)
  }
  const changeHandler = (value: number) => {
    draggingRef.current = false
    props.onChange(value)
  }
  const enableTextField = () => {
    if (props.disabled) return
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
    if (props.disabled) return
    setTextFieldActive(false)
    props.onChange(props.defaultValue)
  }

  const preValueRef = useRef(props.value)
  if (preValueRef.current !== props.value || (!draggingRef.current && value !== props.value)) {
    setValue(props.value)
    preValueRef.current = props.value
  }

  const TextElement = (
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
            <Button disabled={props.disabled} style={{ opacity: 1 }} variant='round' onClick={enableTextField}>
              <EditSvg />
            </Button>
          )}
        </>
      )}
      <Button disabled={props.disabled} style={{ opacity: 1 }} variant='round' onClick={resetHandler}>
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
  )

  return (
    <div
      className='row-with-hover-effect'
      style={{
        flexDirection: props.singleLine ? 'row' : 'column',
        opacity: props.disabled ? 0.3 : 1
      }}
    >
      <div style={{ flex: props.singleLine ? '1 0 0' : '1 0 0', width: props.singleLine ? 'auto' : '100%' }}>
        {TextElement}
      </div>
      {props.singleLine ?
        <div style={{ width: '10rem' }} />
        :
        <div className='horizontal-gap' />
      }
      <div className={props.singleLine ? 'horizontal-gap' : 'vertical-gap'} />
      <div style={{ flex: props.singleLine ? '2 0 0' : '1 0 0', width: props.singleLine ? 'auto' : '100%' }}>
        <Range
          disabled={props.disabled}
          range={props.range}
          value={props.value}
          onChange={changeHandler}
          onUpdate={updateHandler}
        />
      </div>
    </div>
  )
}
