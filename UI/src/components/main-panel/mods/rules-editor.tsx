import TipArea from 'components/base/tip-area'
import { FocusDisabled } from 'cs2/input'
import { Dropdown, DropdownItem, DropdownToggle, PanelFoldout, Scrollable, ScrollableProps } from 'cs2/ui'
import { useTranslate } from 'hooks/translate'
import { CSSProperties, HTMLAttributes, RefAttributes, useCallback, useMemo, useState } from 'react'
import { LaneRulesValue, FieldState, FieldValue, RuleOptionsValue, RuleValue } from 'types'
import { mergeRuleOptionsFieldValues } from 'utils'

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
        <PanelFoldout
          header={
            <FocusDisabled>
              <RuleEditorRow
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
            </FocusDisabled>
          }
        >
          <div className='horizontal-gap' />
          <RuleEditorRow
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
          <RuleEditorRow
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
          <RuleEditorRow
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
        <PanelFoldout
          header={
            <FocusDisabled>
              <RuleEditorRow
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
            </FocusDisabled>
          }
        >
          <div className='horizontal-gap' />
          <RuleEditorRow
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
          <RuleEditorRow
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
          <RuleEditorRow
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
          <RuleEditorRow
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
        <PanelFoldout
          header={
            <FocusDisabled>
              <RuleEditorRow
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
            </FocusDisabled>
          }
        >
          <div className='horizontal-gap' />
          <RuleEditorRow
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
          <RuleEditorRow
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
        <PanelFoldout
          header={
            <FocusDisabled>
              <RuleEditorRow
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
            </FocusDisabled>
          }
        >
          <div className='horizontal-gap' />
          <RuleEditorRow
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
          <RuleEditorRow
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
        <RuleEditorRow
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

function RuleEditorRow(props: {
  name: string
  ruleValue: FieldValue<RuleOptionsValue>
  onChange: (oldValue: FieldValue<RuleOptionsValue>, newValue: FieldValue<RuleOptionsValue>) => void
}) {
  const { t } = useTranslate()

  const handleChange = useCallback((haveFlagRule: RuleValue) => {
    props.onChange(props.ruleValue, {
      state: FieldState.Applied,
      value: {
        hasFlag: haveFlagRule,
      }
    })
  }, [props.onChange])

  return (
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
              if (typeof item !== 'string') {
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
  )
}
