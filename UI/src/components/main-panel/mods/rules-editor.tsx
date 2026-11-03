import TipArea from 'components/base/tip-area'
import RangeRow from 'components/base/range-row'
import { FocusDisabled } from 'cs2/input'
import { Dropdown, DropdownItem, DropdownToggle, PanelFoldout, Scrollable, ScrollableProps } from 'cs2/ui'
import { useTranslate } from 'hooks/translate'
import { CSSProperties, HTMLAttributes, RefAttributes, useCallback, useContext, useMemo, useState } from 'react'
import { LaneRulesValue, FieldState, FieldValue, RuleOptionsValue, RuleValue, UIToolMode } from 'types'
import { UIToolModeContext } from 'context'
import { mergeRuleOptionsFieldValues } from 'utils'
import styled from 'styled-components'

const SmallPanelFoldoutContainer = styled.div`
  font-size: 0.9em;
`

export default function RulesEditor(props: HTMLAttributes<HTMLDivElement> & {
  laneRulesValue: LaneRulesValue
  onValueChange: (oldValue: LaneRulesValue, newValue: LaneRulesValue) => void
}) {
  const { t } = useTranslate()

  const emergencyVehiclesRules = useMemo(() => {
    const { ambulance, fireEngine, policeCar } = props.laneRulesValue.vehicleTypeRules
    let rs;
    rs = mergeRuleOptionsFieldValues(
      ambulance,
      fireEngine,
    )
    rs = mergeRuleOptionsFieldValues(rs, policeCar)
    return rs
  }, [JSON.stringify(props.laneRulesValue.vehicleTypeRules)])

  const serviceVehiclesRules = useMemo(() => {
    const { garbageTruck, hearse, maintenanceVehicle, postVan } = props.laneRulesValue.vehicleTypeRules
    let rs;
    rs = mergeRuleOptionsFieldValues(
      garbageTruck,
      hearse,
    )
    rs = mergeRuleOptionsFieldValues(rs, maintenanceVehicle)
    rs = mergeRuleOptionsFieldValues(rs, postVan)
    return rs
  }, [JSON.stringify(props.laneRulesValue.vehicleTypeRules)])

  const publicVehiclesRules = useMemo(() => {
    const { publicTransport, taxi } = props.laneRulesValue.vehicleTypeRules
    let rs;
    rs = mergeRuleOptionsFieldValues(
      publicTransport,
      taxi,
    )
    return rs
  }, [JSON.stringify(props.laneRulesValue.vehicleTypeRules)])

  const privateVehiclesRules = useMemo(() => {
    const { personalCar, deliveryTruck } = props.laneRulesValue.vehicleTypeRules
    let rs;
    rs = mergeRuleOptionsFieldValues(
      personalCar,
      deliveryTruck,
    )
    return rs
  }, [JSON.stringify(props.laneRulesValue.vehicleTypeRules)])

  const bicycleRules = useMemo(() => {
    const { bicycle } = props.laneRulesValue.vehicleTypeRules
    return bicycle
  }, [JSON.stringify(props.laneRulesValue.vehicleTypeRules)])

  return (
    <div>
      <PanelFoldout header={t('VehicleTypes')} initialExpanded={true}>
        <RuleEditorItem
          name={t('VehicleTypes.EmergencyVehicles')}
          ruleValue={emergencyVehiclesRules}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              vehicleTypeRules: {
                ...props.laneRulesValue.vehicleTypeRules,
                ambulance: newValue,
                fireEngine: newValue,
                policeCar: newValue,
              },
            })
          }}
        />
        <SmallPanelFoldoutContainer>
          <PanelFoldout
            header={t('RulesEditor.Expand')}
          >
            <div className='horizontal-gap' />
            <RuleEditorItem
              name={t('VehicleType.Ambulance')}
              ruleValue={props.laneRulesValue.vehicleTypeRules.ambulance}
              onChange={(_, newValue) => {
                props.onValueChange(props.laneRulesValue, {
                  ...props.laneRulesValue,
                  vehicleTypeRules: {
                    ...props.laneRulesValue.vehicleTypeRules,
                    ambulance: newValue,
                  },
                })
              }}
            />
            <div className='horizontal-divider-with-gap' />
            <RuleEditorItem
              name={t('VehicleType.FireEngine')}
              ruleValue={props.laneRulesValue.vehicleTypeRules.fireEngine}
              onChange={(_, newValue) => {
                props.onValueChange(props.laneRulesValue, {
                  ...props.laneRulesValue,
                  vehicleTypeRules: {
                    ...props.laneRulesValue.vehicleTypeRules,
                    fireEngine: newValue,
                  },
                })
              }}
            />
            <div className='horizontal-divider-with-gap' />
            <RuleEditorItem
              name={t('VehicleType.PoliceCar')}
              ruleValue={props.laneRulesValue.vehicleTypeRules.policeCar}
              onChange={(_, newValue) => {
                props.onValueChange(props.laneRulesValue, {
                  ...props.laneRulesValue,
                  vehicleTypeRules: {
                    ...props.laneRulesValue.vehicleTypeRules,
                    policeCar: newValue,
                  },
                })
              }}
            />
          </PanelFoldout>
        </SmallPanelFoldoutContainer>
        <div className='horizontal-divider-with-gap' />
        <RuleEditorItem
          name={t('VehicleTypes.ServiceVehicles')}
          ruleValue={serviceVehiclesRules}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              vehicleTypeRules: {
                ...props.laneRulesValue.vehicleTypeRules,
                garbageTruck: newValue,
                hearse: newValue,
                maintenanceVehicle: newValue,
                postVan: newValue,
              },
            })
          }}
        />
        <SmallPanelFoldoutContainer>
          <PanelFoldout
            header={t('RulesEditor.Expand')}
          >
            <div className='horizontal-gap' />
            <RuleEditorItem
              name={t('VehicleType.GarbageTruck')}
              ruleValue={props.laneRulesValue.vehicleTypeRules.garbageTruck}
              onChange={(_, newValue) => {
                props.onValueChange(props.laneRulesValue, {
                  ...props.laneRulesValue,
                  vehicleTypeRules: {
                    ...props.laneRulesValue.vehicleTypeRules,
                    garbageTruck: newValue,
                  },
                })
              }}
            />
            <div className='horizontal-divider-with-gap' />
            <RuleEditorItem
              name={t('VehicleType.Hearse')}
              ruleValue={props.laneRulesValue.vehicleTypeRules.hearse}
              onChange={(_, newValue) => {
                props.onValueChange(props.laneRulesValue, {
                  ...props.laneRulesValue,
                  vehicleTypeRules: {
                    ...props.laneRulesValue.vehicleTypeRules,
                    hearse: newValue,
                  },
                })
              }}
            />
            <div className='horizontal-divider-with-gap' />
            <RuleEditorItem
              name={t('VehicleType.MaintenanceVehicle')}
              ruleValue={props.laneRulesValue.vehicleTypeRules.maintenanceVehicle}
              onChange={(_, newValue) => {
                props.onValueChange(props.laneRulesValue, {
                  ...props.laneRulesValue,
                  vehicleTypeRules: {
                    ...props.laneRulesValue.vehicleTypeRules,
                    maintenanceVehicle: newValue,
                  },
                })
              }}
            />
            <div className='horizontal-divider-with-gap' />
            <RuleEditorItem
              name={t('VehicleType.PostVan')}
              ruleValue={props.laneRulesValue.vehicleTypeRules.postVan}
              onChange={(_, newValue) => {
                props.onValueChange(props.laneRulesValue, {
                  ...props.laneRulesValue,
                  vehicleTypeRules: {
                    ...props.laneRulesValue.vehicleTypeRules,
                    postVan: newValue,
                  },
                })
              }}
            />
          </PanelFoldout>
        </SmallPanelFoldoutContainer>
        <div className='horizontal-divider-with-gap' />
        <RuleEditorItem
          name={t('VehicleTypes.PublicVehicles')}
          ruleValue={publicVehiclesRules}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              vehicleTypeRules: {
                ...props.laneRulesValue.vehicleTypeRules,
                publicTransport: newValue,
                taxi: newValue,
              },
            })
          }}
        />
        <SmallPanelFoldoutContainer>
          <PanelFoldout
            header={t('RulesEditor.Expand')}
          >
            <div className='horizontal-gap' />
            <RuleEditorItem
              name={t('VehicleType.PublicTransport')}
              ruleValue={props.laneRulesValue.vehicleTypeRules.publicTransport}
              onChange={(_, newValue) => {
                props.onValueChange(props.laneRulesValue, {
                  ...props.laneRulesValue,
                  vehicleTypeRules: {
                    ...props.laneRulesValue.vehicleTypeRules,
                    publicTransport: newValue,
                  },
                })
              }}
            />
            <div className='horizontal-divider-with-gap' />
            <RuleEditorItem
              name={t('VehicleType.Taxi')}
              ruleValue={props.laneRulesValue.vehicleTypeRules.taxi}
              onChange={(_, newValue) => {
                props.onValueChange(props.laneRulesValue, {
                  ...props.laneRulesValue,
                  vehicleTypeRules: {
                    ...props.laneRulesValue.vehicleTypeRules,
                    taxi: newValue,
                  },
                })
              }}
            />
          </PanelFoldout>
        </SmallPanelFoldoutContainer>
        <div className='horizontal-divider-with-gap' />
        <RuleEditorItem
          name={t('VehicleTypes.PrivateVehicles')}
          ruleValue={privateVehiclesRules}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              vehicleTypeRules: {
                ...props.laneRulesValue.vehicleTypeRules,
                personalCar: newValue,
                deliveryTruck: newValue,
              },
            })
          }}
        />
        <SmallPanelFoldoutContainer>
          <PanelFoldout
            header={t('RuleEditor.Expand')}
          >
            <div className='horizontal-gap' />
            <RuleEditorItem
              name={t('VehicleType.DeliveryTruck')}
              ruleValue={props.laneRulesValue.vehicleTypeRules.deliveryTruck}
              onChange={(_, newValue) => {
                props.onValueChange(props.laneRulesValue, {
                  ...props.laneRulesValue,
                  vehicleTypeRules: {
                    ...props.laneRulesValue.vehicleTypeRules,
                    deliveryTruck: newValue,
                  },
                })
              }}
            />
            <div className='horizontal-divider-with-gap' />
            <RuleEditorItem
              name={t('VehicleType.PersonalCar')}
              ruleValue={props.laneRulesValue.vehicleTypeRules.personalCar}
              onChange={(_, newValue) => {
                props.onValueChange(props.laneRulesValue, {
                  ...props.laneRulesValue,
                  vehicleTypeRules: {
                    ...props.laneRulesValue.vehicleTypeRules,
                    personalCar: newValue,
                  },
                })
              }}
            />
          </PanelFoldout>
        </SmallPanelFoldoutContainer>
        <div className='horizontal-divider-with-gap' />
        <RuleEditorItem
          name={t('VehicleType.Bicycle')}
          ruleValue={bicycleRules}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              vehicleTypeRules: {
                ...props.laneRulesValue.vehicleTypeRules,
                bicycle: newValue,
              },
            })
          }}
        />
      </PanelFoldout>
    </div>
  )
}

const DropdownToggleStyle: CSSProperties = {
  width: '8em',
  margin: '0 0.5em',
}

function RuleEditorItem(props: {
  name: string
  ruleValue: FieldValue<RuleOptionsValue>
  onChange: (oldValue: FieldValue<RuleOptionsValue>, newValue: FieldValue<RuleOptionsValue>) => void
}) {
  const { t } = useTranslate()
  const [mode] = useContext(UIToolModeContext)

  const handleChange = useCallback((haveFlagRule: RuleValue) => {
    props.onChange(props.ruleValue, {
      state: FieldState.Applied,
      value: {
        ...props.ruleValue.value,
        hasFlag: haveFlagRule,
      }
    })
  }, [props.onChange, props.ruleValue])

  const handleFixedCostChange = useCallback((fixedCost: number) => {
    props.onChange(props.ruleValue, {
      state: FieldState.Applied,
      value: {
        ...props.ruleValue.value,
        fixedCost: Math.min(65535, Math.max(0, Math.round(fixedCost))),
      },
    })
  }, [props.onChange, props.ruleValue])

  return (
    <div
      style={{
        width: '100%',
      }}
    >
      <div className='row-with-hover-effect'>
        <div
          style={{
            flex: '1',
          }}
        >
          {props.name}
        </div>
        <div
          style={{
            flex: '1 0 1em',
            textAlign: 'right',
          }}
        >
          {(props.ruleValue.state === FieldState.PartiallyApplied) && <>*</>}
        </div>
        <Dropdown
          content={
            <>
              {Object.values(RuleValue).map((item) => {
                if (typeof item !== 'string' || (mode !== UIToolMode.MasterLane && item === 'FixedCost')) {
                  return null
                }
                return (
                  <DropdownItem
                    value={item}
                    key={item}
                    onChange={(value) => handleChange(RuleValue[value as keyof typeof RuleValue])}
                  >
                    <div>{t(`Rule.${item}`)}</div>
                  </DropdownItem>
                )
              })}
            </>
          }
        >
          <TipArea position={'right'} tooltip={t('RuleEditor.HaveFlagRuleTooltip')}>
            <DropdownToggle style={DropdownToggleStyle}>
              {t(
                `Rule.${Object.keys(RuleValue).find((key) => RuleValue[key as keyof typeof RuleValue] === props.ruleValue.value.hasFlag)}`,
              )}
            </DropdownToggle>
          </TipArea>
        </Dropdown>
      </div>
      <RangeRow
        disabled={mode !== UIToolMode.MasterLane || props.ruleValue.value.hasFlag !== RuleValue.FixedCost}
        singleLine
        onChange={handleFixedCostChange}
        label={t('Rule.FixedCost')}
        value={props.ruleValue.value.fixedCost}
        valuePrefix={props.ruleValue.state === FieldState.PartiallyApplied ? '*' : ''}
        valueSuffix=""
        defaultValue={0}
        enableTextField
        textFieldRegExp="^[0-9]*$"
        min={0}
        max={65535}
        step={1}
      />
    </div>
  )
}
