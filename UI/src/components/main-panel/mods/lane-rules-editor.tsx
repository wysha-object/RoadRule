import TipArea from 'components/base/tip-area'
import { Dropdown, DropdownItem, DropdownToggle, PanelFoldout, Scrollable, ScrollableProps } from 'cs2/ui'
import { useTranslate } from 'hooks/translate'
import { CSSProperties, HTMLAttributes, RefAttributes, useCallback, useMemo, useState } from 'react'
import { LaneRulesValue, Rule, FieldState, FieldValue } from 'types'

export default function RulesEditor(props: HTMLAttributes<HTMLDivElement> & {
  laneRulesValue: LaneRulesValue
  onValueChange: (oldValue: LaneRulesValue, newValue: LaneRulesValue) => void
}) {
  const { t } = useTranslate()

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
        <RuleEditorRow
          name={t('EnergyTypes.FuelAndElectricity')}
          ruleValue={props.laneRulesValue.energyTypesRules.fuelAndElectricity}
          onChange={(_, newValue) => {
            props.onValueChange(props.laneRulesValue, {
              ...props.laneRulesValue,
              energyTypesRules: {
                ...props.laneRulesValue.energyTypesRules,
                fuelAndElectricity: newValue,
              },
            })
          }}
        />
      </PanelFoldout>
      <PanelFoldout header={t('VehicleType')} initialExpanded={true}>
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
    </div>
  )
}

enum DropdownItemValue {
  None,
  Prefer,
  Forbidden
}

const DropdownToggleStyle: CSSProperties = {
  width: '8em',
  margin: '0 0.5em',
}

function RuleEditorRow(props: {
  name: string
  ruleValue: FieldValue<Rule>
  onChange: (oldValue: FieldValue<Rule>, newValue: FieldValue<Rule>) => void
}) {
  const { t } = useTranslate()

  const { noneFlagRule, haveFlagRule } = useMemo(() => {
    switch (props.ruleValue.value) {
      case Rule.None:
        return { noneFlagRule: DropdownItemValue.None, haveFlagRule: DropdownItemValue.None }
      case Rule.PreferOrNone:
        return { noneFlagRule: DropdownItemValue.Prefer, haveFlagRule: DropdownItemValue.None }
      case Rule.NoneOrPrefer:
        return { noneFlagRule: DropdownItemValue.None, haveFlagRule: DropdownItemValue.Prefer }
      case Rule.ForbiddenOrNone:
        return { noneFlagRule: DropdownItemValue.Forbidden, haveFlagRule: DropdownItemValue.None }
      case Rule.NoneOrForbidden:
        return { noneFlagRule: DropdownItemValue.None, haveFlagRule: DropdownItemValue.Forbidden }
      case Rule.ForbiddenOrPrefer:
        return { noneFlagRule: DropdownItemValue.Forbidden, haveFlagRule: DropdownItemValue.Prefer }
      case Rule.PreferOrForbidden:
        return { noneFlagRule: DropdownItemValue.Prefer, haveFlagRule: DropdownItemValue.Forbidden }
    }
  }, [props.ruleValue.value])

  const handleChange = useCallback((noneFlagRule: DropdownItemValue, haveFlagRule: DropdownItemValue) => {
    switch (noneFlagRule) {
      case DropdownItemValue.None:
        switch (haveFlagRule) {
          case DropdownItemValue.None:
            props.onChange(props.ruleValue, { state: FieldState.Applied, value: Rule.None })
            break
          case DropdownItemValue.Prefer:
            props.onChange(props.ruleValue, { state: FieldState.Applied, value: Rule.NoneOrPrefer })
            break
          case DropdownItemValue.Forbidden:
            props.onChange(props.ruleValue, { state: FieldState.Applied, value: Rule.NoneOrForbidden })
            break
        }
        break
      case DropdownItemValue.Prefer:
        switch (haveFlagRule) {
          case DropdownItemValue.None:
            props.onChange(props.ruleValue, { state: FieldState.Applied, value: Rule.PreferOrNone })
            break
          case DropdownItemValue.Prefer:
            // Invalid combination
            break
          case DropdownItemValue.Forbidden:
            props.onChange(props.ruleValue, { state: FieldState.Applied, value: Rule.PreferOrForbidden })
            break
        }
        break
      case DropdownItemValue.Forbidden:
        switch (haveFlagRule) {
          case DropdownItemValue.None:
            props.onChange(props.ruleValue, { state: FieldState.Applied, value: Rule.ForbiddenOrNone })
            break
          case DropdownItemValue.Prefer:
            props.onChange(props.ruleValue, { state: FieldState.Applied, value: Rule.ForbiddenOrPrefer })
            break
          case DropdownItemValue.Forbidden:
            // Invalid combination
            break
        }
    }
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
        {props.ruleValue.state === FieldState.PartiallyApplied && <>!</>}
      </div>
      <Dropdown
        content={
          <>
            {Object.values(DropdownItemValue).map((item) => {
              if (typeof item !== 'string') {
                return null
              }
              return (
                <DropdownItem
                  value={item}
                  key={item}
                  onChange={(value) => handleChange(DropdownItemValue[value as keyof typeof DropdownItemValue], haveFlagRule)}
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
              `Rule.${Object.keys(DropdownItemValue).find((key) => DropdownItemValue[key as keyof typeof DropdownItemValue] === noneFlagRule)}`,
            )}
          </DropdownToggle>
        </TipArea>
      </Dropdown>
      <Dropdown
        content={
          <>
            {Object.values(DropdownItemValue).map((item) => {
              if (typeof item !== 'string') {
                return null
              }
              return (
                <DropdownItem
                  value={item}
                  key={item}
                  onChange={(value) => handleChange(noneFlagRule, DropdownItemValue[value as keyof typeof DropdownItemValue])}
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
              `Rule.${Object.keys(DropdownItemValue).find((key) => DropdownItemValue[key as keyof typeof DropdownItemValue] === haveFlagRule)}`,
            )}
          </DropdownToggle>
        </TipArea>
      </Dropdown>
    </div>
  )
}
