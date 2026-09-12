import RangeRow from "components/base/range-row";
import { PanelFoldout } from "cs2/ui";
import { useGetCompositionCmd } from "hooks/cmd";
import { useTranslate } from "hooks/translate";
import { HTMLAttributes } from "react";
import { CarLaneValue, FieldState } from "types";

export default function CarLaneEditor(props: HTMLAttributes<HTMLDivElement> & {
    lanePropertiesValue: CarLaneValue
    onValueChange: (oldValue: CarLaneValue, newValue: CarLaneValue) => void
}) {
    const { t } = useTranslate()
    const { speedLimit: defaultSpeedLimit } = useGetCompositionCmd()
    return (
        <div>
            <PanelFoldout header={t("CarLane")} initialExpanded={true}>
                <RangeRow
                    onChange={function (value: number): void {
                        value = value
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
                    value={props.lanePropertiesValue.speedLimit.value}
                    valuePrefix={props.lanePropertiesValue.speedLimit.state === FieldState.PartiallyApplied ? '!' : ''}
                    valueSuffix={""}
                    defaultValue={defaultSpeedLimit}
                    min={30}
                    max={200}
                    step={10}
                />
            </PanelFoldout>
        </div>
    )
}