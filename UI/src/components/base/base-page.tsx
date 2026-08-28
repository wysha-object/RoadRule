import React, { DetailedHTMLProps, forwardRef, HTMLAttributes } from 'react'

export interface BasePageProps extends DetailedHTMLProps<
  HTMLAttributes<HTMLDivElement>,
  HTMLDivElement
> {
  header?: React.ReactNode
  footer?: React.ReactNode
  children?: React.ReactNode
}

const BasePage = forwardRef<HTMLDivElement, BasePageProps>(
  function BasePage(props, ref) {
    return (
      <div
        ref={ref}
        {...props}
        style={{
          maxHeight: '50em',
          top: 'calc(10rem + var(--floatingToggleSize))',
          borderRadius: '1em',
          position: 'fixed',
          left: '0',
          zIndex: '1000',
          overflow: 'hidden',
          color: 'var(--textColor)',
          backgroundImage:
            'linear-gradient( var(--panelGradientStart) , var(--panelGradientEnd) )',
          ...props.style,
        }}
      >
        {props.header && (
          <div
            style={{
              height: '2em',
              backgroundColor: 'var(--panelColorDark)',
              color: 'var(--accentColorNormal)',
            }}
          >
            {props.header}
          </div>
        )}
        {props.children}
        {props.footer && (
          <div
            style={{
              height: '2em',
              backgroundColor: 'var(--panelColorDark)',
            }}
          >
            {props.footer}
          </div>
        )}
      </div>
    )
  },
)
export default BasePage
