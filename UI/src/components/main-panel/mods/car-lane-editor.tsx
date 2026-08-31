import RangeRow from "components/base/range-row";
import { PanelFoldout } from "cs2/ui";
import { useTranslate } from "hooks/translate";
import { HTMLAttributes } from "react";
import { CarLaneValue, FieldState } from "types";

export default function CarLaneEditor(props: HTMLAttributes<HTMLDivElement> & {
    lanePropertiesValue: CarLaneValue
    onValueChange: (oldValue: CarLaneValue, newValue: CarLaneValue) => void
}) {
    const { t } = useTranslate()
    return (
        <div>
            <PanelFoldout header={t("CarLane")} initialExpanded={true}>
                <RangeRow
                    onChange={function (value: number): void {
                        value = value / 2
                        props.onValueChange(
                            props.lanePropertiesValue,
                            {
                                ...props.lanePropertiesValue,
                                speedLimit: {
                                    state: FieldState.Applied,
                                    value: value,
                                },
                            },
                        )
                    }}
                    label={t("CarLane.SpeedLimit")}
                    value={props.lanePropertiesValue.speedLimit.value * 2}
                    valuePrefix={props.lanePropertiesValue.speedLimit.state === FieldState.PartiallyApplied ? '!' : ''}
                    valueSuffix={""}
                    defaultValue={props.lanePropertiesValue.defaultSpeedLimit.value * 2}
                    min={30}
                    max={300}
                    step={10}
                />
            </PanelFoldout>
        </div>
    )
}