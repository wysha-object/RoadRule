import BasePage from "components/base/base-page";
import { CSSProperties, HTMLAttributes, useMemo } from "react";
import { FieldState, Lane, RuleValue } from "types";
import { mergeRuleOptionsFieldValues } from "utils";
import InfosignEmergencyPrefer from 'assets/images/infosign-emergency-prefer.svg'
import InfosignEmergencyForbidden from 'assets/images/infosign-emergency-forbidden.svg'
import InfosignEmergencyDisallow from 'assets/images/infosign-emergency-disallow.svg'
import InfosignServicePrefer from 'assets/images/infosign-service-prefer.svg'
import InfosignServiceForbidden from 'assets/images/infosign-service-forbidden.svg'
import InfosignServiceDisallow from 'assets/images/infosign-service-disallow.svg'
import InfosignPublicPrefer from 'assets/images/infosign-public-prefer.svg'
import InfosignPublicForbidden from 'assets/images/infosign-public-forbidden.svg'
import InfosignPublicDisallow from 'assets/images/infosign-public-disallow.svg'
import InfosignPrivatePrefer from 'assets/images/infosign-private-prefer.svg'
import InfosignPrivateForbidden from 'assets/images/infosign-private-forbidden.svg'
import InfosignPrivateDisallow from 'assets/images/infosign-private-disallow.svg'
import InfosignBicyclePrefer from 'assets/images/infosign-bicycle-prefer.svg'
import InfosignBicycleForbidden from 'assets/images/infosign-bicycle-forbidden.svg'
import InfosignBicycleDisallow from 'assets/images/infosign-bicycle-disallow.svg'
import InfosignSpeedLimit from 'assets/images/infosign-speed-limit.svg'

export default function LanePage(props: {
    lane: Lane,
    defaultSpeedLimit: number,
    style?: CSSProperties
}) {
    const emergencyVehiclesRules = useMemo(() => {
        const { ambulance, fireEngine, policeCar } = props.lane.laneRules.vehicleTypeRules
        let rs;
        rs = mergeRuleOptionsFieldValues(
            ambulance,
            fireEngine,
        )
        rs = mergeRuleOptionsFieldValues(rs, policeCar)
        return rs
    }, [JSON.stringify(props.lane.laneRules.vehicleTypeRules)])

    const serviceVehiclesRules = useMemo(() => {
        const { garbageTruck, hearse, maintenanceVehicle, postVan } = props.lane.laneRules.vehicleTypeRules
        let rs;
        rs = mergeRuleOptionsFieldValues(
            garbageTruck,
            hearse,
        )
        rs = mergeRuleOptionsFieldValues(rs, maintenanceVehicle)
        rs = mergeRuleOptionsFieldValues(rs, postVan)
        return rs
    }, [JSON.stringify(props.lane.laneRules.vehicleTypeRules)])

    const publicVehiclesRules = useMemo(() => {
        const { publicTransport, taxi } = props.lane.laneRules.vehicleTypeRules
        let rs;
        rs = mergeRuleOptionsFieldValues(
            publicTransport,
            taxi,
        )
        return rs
    }, [JSON.stringify(props.lane.laneRules.vehicleTypeRules)])

    const privateVehiclesRules = useMemo(() => {
        const { personalCar, deliveryTruck } = props.lane.laneRules.vehicleTypeRules
        let rs;
        rs = mergeRuleOptionsFieldValues(
            personalCar,
            deliveryTruck,
        )
        return rs
    }, [JSON.stringify(props.lane.laneRules.vehicleTypeRules)])

    const bicycleVehiclesRules = props.lane.laneRules.vehicleTypeRules.bicycle

    const emergencyRule = emergencyVehiclesRules.value.hasFlag;

    const serviceRule = serviceVehiclesRules.value.hasFlag;

    const publicRule = publicVehiclesRules.value.hasFlag;

    const privateRule = privateVehiclesRules.value.hasFlag;

    const bicycleRule = bicycleVehiclesRules.value.hasFlag;

    return (
        <BasePage
            style={props.style}
        >
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                {props.lane.carLane.speedLimit.value !== props.defaultSpeedLimit &&
                    <div style={{ display: 'flex', margin: '0.5em' }}>
                        <div style={{ ...InfosignStyle, position: 'relative' }}>
                            <InfosignSpeedLimit style={{
                                width: '100%',
                                height: '100%',
                                position: 'absolute',
                                top: '50%',
                                left: '50%',
                                transform: 'translate(-50%, -50%)'
                            }} />
                            <div style={{
                                color: 'black',
                                position: 'absolute',
                                top: '50%',
                                left: '50%',
                                transform: 'translate(-50%, -50%)'
                            }}>
                                {Math.round(props.lane.carLane.speedLimit.value)}
                            </div>
                        </div>
                    </div>
                }
                {emergencyRule !== RuleValue.None &&
                    <div style={{ display: 'flex', margin: '0.5em' }}>
                        {emergencyRule === RuleValue.Prefer && <InfosignEmergencyPrefer style={InfosignStyle} />}
                        {emergencyRule === RuleValue.Forbidden && <InfosignEmergencyForbidden style={InfosignStyle} />}
                        {emergencyRule === RuleValue.Disallow && <InfosignEmergencyDisallow style={InfosignStyle} />}
                        <Star show={emergencyVehiclesRules.state === FieldState.PartiallyApplied} />
                    </div>
                }
                {serviceRule !== RuleValue.None &&
                    <div style={{ display: 'flex', margin: '0.5em' }}>
                        {serviceRule === RuleValue.Prefer && <InfosignServicePrefer style={InfosignStyle} />}
                        {serviceRule === RuleValue.Forbidden && <InfosignServiceForbidden style={InfosignStyle} />}
                        {serviceRule === RuleValue.Disallow && <InfosignServiceDisallow style={InfosignStyle} />}
                        <Star show={serviceVehiclesRules.state === FieldState.PartiallyApplied} />
                    </div>
                }
                {publicRule !== RuleValue.None &&
                    <div style={{ display: 'flex', margin: '0.5em' }}>
                        {publicRule === RuleValue.Prefer && <InfosignPublicPrefer style={InfosignStyle} />}
                        {publicRule === RuleValue.Forbidden && <InfosignPublicForbidden style={InfosignStyle} />}
                        {publicRule === RuleValue.Disallow && <InfosignPublicDisallow style={InfosignStyle} />}
                        <Star show={publicVehiclesRules.state === FieldState.PartiallyApplied} />
                    </div>
                }
                {privateRule !== RuleValue.None &&
                    <div style={{ display: 'flex', margin: '0.5em' }}>
                        {privateRule === RuleValue.Prefer && <InfosignPrivatePrefer style={InfosignStyle} />}
                        {privateRule === RuleValue.Forbidden && <InfosignPrivateForbidden style={InfosignStyle} />}
                        {privateRule === RuleValue.Disallow && <InfosignPrivateDisallow style={InfosignStyle} />}
                        <Star show={privateVehiclesRules.state === FieldState.PartiallyApplied} />
                    </div>
                }
                {bicycleRule !== RuleValue.None &&
                    <div style={{ display: 'flex', margin: '0.5em' }}>
                        {bicycleRule === RuleValue.Prefer && <InfosignBicyclePrefer style={InfosignStyle} />}
                        {bicycleRule === RuleValue.Forbidden && <InfosignBicycleForbidden style={InfosignStyle} />}
                        {bicycleRule === RuleValue.Disallow && <InfosignBicycleDisallow style={InfosignStyle} />}
                        <Star show={bicycleVehiclesRules.state === FieldState.PartiallyApplied} />
                    </div>
                }
            </div>
        </BasePage>
    );
}

function Star(props: { show: boolean }) {
    return <div style={{ width: '1rem' }}>{props.show && '*'}</div>;
}

const InfosignStyle: CSSProperties = {
    width: '32rem',
    height: '32rem'
}