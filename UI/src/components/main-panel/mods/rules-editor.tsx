import { Dropdown, DropdownItem, DropdownToggle, PanelFoldout } from "cs2/ui"
import { useTranslate } from "hooks/translate"
import { LaneRulesValue, Rule, RuleState, RuleValue } from "types"

export default function RulesEditor(
    props: {
        laneRulesValue: LaneRulesValue,
        onChange: (oldValue: LaneRulesValue, newValue: LaneRulesValue) => void
    }
) {
    const { t } = useTranslate()

    return (
        <div>
            <PanelFoldout header={t('CarFlags')} initialExpanded={true}>
                <RuleEditor
                    name={t('CarFlags.Emergency')}
                    ruleValue={props.laneRulesValue.carFlagsRules.emergency}
                    onChange={(_, newValue) => {
                        props.onChange(
                            props.laneRulesValue,
                            {
                                ...props.laneRulesValue,
                                carFlagsRules: {
                                    ...props.laneRulesValue.carFlagsRules,
                                    emergency: newValue,
                                }
                            }
                        )
                    }}
                />
            </PanelFoldout>
            <PanelFoldout header={t('SizeClass')} initialExpanded={true}>
                <RuleEditor
                    name={t('SizeClass.Small')}
                    ruleValue={props.laneRulesValue.sizeClassRules.small}
                    onChange={(_, newValue) => {
                        props.onChange(
                            props.laneRulesValue,
                            {
                                ...props.laneRulesValue,
                                sizeClassRules: {
                                    ...props.laneRulesValue.sizeClassRules,
                                    small: newValue,
                                }
                            }
                        )
                    }}
                />
                <RuleEditor
                    name={t('SizeClass.Medium')}
                    ruleValue={props.laneRulesValue.sizeClassRules.medium}
                    onChange={(_, newValue) => {
                        props.onChange(
                            props.laneRulesValue,
                            {
                                ...props.laneRulesValue,
                                sizeClassRules: {
                                    ...props.laneRulesValue.sizeClassRules,
                                    medium: newValue,
                                }
                            }
                        )
                    }}
                />
                <RuleEditor
                    name={t('SizeClass.Large')}
                    ruleValue={props.laneRulesValue.sizeClassRules.large}
                    onChange={(_, newValue) => {
                        props.onChange(
                            props.laneRulesValue,
                            {
                                ...props.laneRulesValue,
                                sizeClassRules: {
                                    ...props.laneRulesValue.sizeClassRules,
                                    large: newValue,
                                }
                            }
                        )
                    }}
                />
            </PanelFoldout>
            <PanelFoldout header={t('EnergyTypes')} initialExpanded={true}>
                <RuleEditor
                    name={t('EnergyTypes.Fuel')}
                    ruleValue={props.laneRulesValue.energyTypesRules.fuel}
                    onChange={(_, newValue) => {
                        props.onChange(
                            props.laneRulesValue,
                            {
                                ...props.laneRulesValue,
                                energyTypesRules: {
                                    ...props.laneRulesValue.energyTypesRules,
                                    fuel: newValue,
                                }
                            }
                        )
                    }}
                />
                <RuleEditor
                    name={t('EnergyTypes.Electricity')}
                    ruleValue={props.laneRulesValue.energyTypesRules.electricity}
                    onChange={(_, newValue) => {
                        props.onChange(
                            props.laneRulesValue,
                            {
                                ...props.laneRulesValue,
                                energyTypesRules: {
                                    ...props.laneRulesValue.energyTypesRules,
                                    electricity: newValue,
                                }
                            }
                        )
                    }}
                />
                <RuleEditor
                    name={t('EnergyTypes.FuelAndElectricity')}
                    ruleValue={props.laneRulesValue.energyTypesRules.fuelAndElectricity}
                    onChange={(_, newValue) => {
                        props.onChange(
                            props.laneRulesValue,
                            {
                                ...props.laneRulesValue,
                                energyTypesRules: {
                                    ...props.laneRulesValue.energyTypesRules,
                                    fuelAndElectricity: newValue,
                                }
                            }
                        )
                    }}
                />
            </PanelFoldout>
        </div>
    )
}

function RuleEditor(
    props: {
        name: string,
        ruleValue: RuleValue,
        onChange: (oldValue: RuleValue, newValue: RuleValue) => void
    }
) {
    const { t } = useTranslate()

    return (
        <div className="row">
            <div style={{
                flex: '1',
            }}>
                {props.name}
            </div>
            <div style={{
                flex: '1 0 1em'
            }}>
                {props.ruleValue.state === RuleState.PartiallyApplied && (
                    <>
                        !
                    </>
                )}
            </div>
            <div>
                <Dropdown
                    content={
                        <>
                            {Object.values(Rule).map(rule => {
                                if (typeof rule !== 'string') {
                                    return null
                                }
                                return (
                                    <DropdownItem value={rule} key={rule} onChange={(value) => {
                                        props.onChange(
                                            props.ruleValue,
                                            {
                                                state: RuleState.Applied,
                                                rule: Rule[value as keyof typeof Rule],
                                            }
                                        )
                                    }}>
                                        <div>
                                            {t(`Rule.${rule}`)}
                                        </div>
                                    </DropdownItem>
                                )
                            })}
                        </>
                    }
                >
                    <DropdownToggle>
                        {t(`Rule.${Object.keys(Rule).find(key => Rule[key as keyof typeof Rule] === props.ruleValue.rule)}`)}
                    </DropdownToggle>
                </Dropdown>
            </div>
        </div>
    )
}