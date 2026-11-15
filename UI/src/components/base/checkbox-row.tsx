import TipArea from 'components/base/tip-area'
import TipIcon from 'components/icon/tip-icon'
import { ReactNode } from 'react'
import styled from 'styled-components'

function Checkbox(props: { isChecked: boolean }) {
  return (
    <div
      style={{
        borderWidth: '2rem',
        borderStyle: 'solid',
        borderBottomColor: 'var(--textColorDimmer)',
        borderLeftColor: 'var(--textColorDimmer)',
        borderRightColor: 'var(--textColorDimmer)',
        borderTopColor: 'var(--textColorDimmer)',
        borderRadius: '3rem',
        margin: '0 0.5em 0 0',
        width: '1em',
        height: '1em',
        padding: '1rem',
      }}
    >
      <div
        style={{
          width: '100%',
          height: '100%',
          maskSize: '100% auto',
          backgroundColor: 'var(--textColor)',
          opacity: props.isChecked ? 1 : 0,
          maskImage: 'url(Media/Glyphs/Checkmark.svg)'
        }}
      />
    </div>
  )
}


export default function CheckboxRow(props: {
  isChecked: boolean
  label: string
  tooltip: ReactNode
  onClick: () => void
}) {
  return (
    <TipArea
      className='row-with-hover-effect'
      onClick={props.onClick}
      position='right-start'
      tooltip={props.tooltip}
    >
      <div style={{ flex: '1', display: 'flex', alignItems: 'center' }}>
        <Checkbox isChecked={props.isChecked} />
        <div>{props.label}</div>
      </div>
      <div className='vertical-gap' />
      <TipIcon />
    </TipArea>
  )
}
