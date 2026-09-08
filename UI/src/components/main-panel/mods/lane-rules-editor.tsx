import TipArea from 'components/base/tip-area'
import { FocusDisabled } from 'cs2/input'
import { Dropdown, DropdownItem, DropdownToggle, PanelFoldout, Scrollable, ScrollableProps } from 'cs2/ui'
import { useTranslate } from 'hooks/translate'
import { CSSProperties, HTMLAttributes, RefAttributes, useCallback, useMemo, useState } from 'react'
import { LaneRulesValue, FieldState, FieldValue, RuleOptionsValue, RuleValue } from 'types'
import { mergeRuleValues } from 'utils'

export default function RulesEditor(props: HTMLAttributes<HTMLDivElement> & {
  laneRulesValue: LaneRulesValue
  onValueChange: (oldValue: LaneRulesValue, newValue: LaneRulesValue) => void
}) {
  const { t } = useTranslate()

  const emergencyVehiclesRules = useMemo(() => {
    const { ambulance, fireEngine, policeCar } = props.laneRulesValue.vehicleTypeRules
    let rs;
    rs = mergeRuleValues(
      ambulance,
      fireEngine,
    )
    rs = mergeRuleValues(rs, policeCar)
    return rs
  }, [JSON.stringify(props.laneRulesValue.vehicleTypeRules)])

  const serviceVehiclesRules = useMemo(() => {
    const { garbageTruck, hearse, maintenanceVehicle, postVan } = props.laneRulesValue.vehicleTypeRules
    let rs;
    rs = mergeRuleValues(
      garbageTruck,
      hearse,
    )
    rs = mergeRuleValues(rs, maintenanceVehicle)
    rs = mergeRuleValues(rs, postVan)
    return rs
  }, [JSON.stringify(props.laneRulesValue.vehicleTypeRules)])

  const publicVehiclesRules = useMemo(() => {
    const { publicTransport, taxi } = props.laneRulesValue.vehicleTypeRules
    let rs;
    rs = mergeRuleValues(
      publicTransport,
      taxi,
    )
    return rs
  }, [JSON.stringify(props.laneRulesValue.vehicleTypeRules)])

  const privateVehiclesRules = useMemo(() => {
    const { personalCar, deliveryTruck } = props.laneRulesValue.vehicleTypeRules
    let rs;
    rs = mergeRuleValues(
      personalCar,
      deliveryTruck,
    )
    return rs
  }, [JSON.stringify(props.laneRulesValue.vehicleTypeRules)])

  return (
    <div>
      <PanelFoldout header={t('CarFlags')} initialExpanded={true}>
        <RuleEditorRow
          name={t('CarFlags.Emergency')}
          ruleValue={props.laneRulesValue.carFlagsRules.emergency}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              carFlagsRules: {
                ...props.laneRulesValue.carFlagsRules,
                emergency: newValue,
              },
            })
          }}
        />
      </PanelFoldout>
      <PanelFoldout header={t('SizeClass')} initialExpanded={true}>
        <RuleEditorRow
          name={t('SizeClass.Small')}
          ruleValue={props.laneRulesValue.sizeClassRules.small}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              sizeClassRules: {
                ...props.laneRulesValue.sizeClassRules,
                small: newValue,
              },
            })
          }}
        />
        <RuleEditorRow
          name={t('SizeClass.Medium')}
          ruleValue={props.laneRulesValue.sizeClassRules.medium}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              sizeClassRules: {
                ...props.laneRulesValue.sizeClassRules,
                medium: newValue,
              },
            })
          }}
        />
        <RuleEditorRow
          name={t('SizeClass.Large')}
          ruleValue={props.laneRulesValue.sizeClassRules.large}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              sizeClassRules: {
                ...props.laneRulesValue.sizeClassRules,
                large: newValue,
              },
            })
          }}
        />
      </PanelFoldout>
      <PanelFoldout header={t('EnergyTypes')} initialExpanded={true}>
        <RuleEditorRow
          name={t('EnergyTypes.Fuel')}
          ruleValue={props.laneRulesValue.energyTypesRules.fuel}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              energyTypesRules: {
                ...props.laneRulesValue.energyTypesRules,
                fuel: newValue,
              },
            })
          }}
        />
        <RuleEditorRow
          name={t('EnergyTypes.Electricity')}
          ruleValue={props.laneRulesValue.energyTypesRules.electricity}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              energyTypesRules: {
                ...props.laneRulesValue.energyTypesRules,
                electricity: newValue,
              },
            })
          }}
        />
      </PanelFoldout>
      <PanelFoldout header={t('VehicleTypes')} initialExpanded={true}>
        <PanelFoldout
          header={
            <FocusDisabled>
              <RuleEditorRow
                name={t('VehicleTypes.EmergencyVehicles')}
                ruleValue={emergencyVehiclesRules}
                combined={true}
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
                combined={true}
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
                combined={true}
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
                combined={true}
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
  combined?: boolean
  onChange: (oldValue: FieldValue<RuleOptionsValue>, newValue: FieldValue<RuleOptionsValue>) => void
}) {
  const { t } = useTranslate()

  const handleChange = useCallback((noneFlagRule: RuleValue, haveFlagRule: RuleValue) => {
    if (props.combined) {
      noneFlagRule = RuleValue.None
    }
    props.onChange(props.ruleValue, {
      state: FieldState.Applied,
      value: {
        noFlag: noneFlagRule,
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
        {((props.ruleValue.state === FieldState.PartiallyApplied) || (props.combined && props.ruleValue.value.noFlag !== RuleValue.None))  && <>!</>}
      </div>
      {!props.combined &&
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
                    onChange={(value) => handleChange(RuleValue[value as keyof typeof RuleValue], props.ruleValue.value.hasFlag)}
                  >
                    <div>{t(`Rule.${item}`)}</div>
                  </DropdownItem>
                )
              })}
            </>
          }
        >
          <TipArea position={'right'} tooltip={t('RuleEditor.NoneFlagRuleTooltip')}>
            <DropdownToggle style={DropdownToggleStyle}>
              {t(
                `Rule.${Object.keys(RuleValue).find((key) => RuleValue[key as keyof typeof RuleValue] === props.ruleValue.value.noFlag)}`,
              )}
            </DropdownToggle>
          </TipArea>
        </Dropdown>
      }
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
                  onChange={(value) => handleChange(props.ruleValue.value.noFlag, RuleValue[value as keyof typeof RuleValue])}
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
