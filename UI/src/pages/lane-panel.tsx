import BasePage from "components/base/base-page";
import LanePage from "components/lane-panel/lane-page";
import { UIToolModeContext } from "context";
import { useGetAllLaneCmd, useGetAllLaneGroupCmd, useGetToolStateCmd } from "hooks/cmd";
import { useContext, useMemo } from "react";
import { ToolState, UIToolMode } from "types";
import { MAX_DISPLAY_DISTANCE } from "utils";

export default function LanePanel() {
  const allLaneGroup = useGetAllLaneGroupCmd()
  const allLane = useGetAllLaneCmd()
  const toolState = useGetToolStateCmd()

  const [mode] = useContext(UIToolModeContext)

  const lanes = mode === UIToolMode.MasterLane ? allLaneGroup : allLane

  const usedZIndex = new Set<number>()

  return (
    <>
      {
        toolState !== ToolState.Disabled
          ? Object
            .entries(lanes)
            .sort((a, b) => {
              if (a[1].lane.screenPoint.distance !== b[1].lane.screenPoint.distance) {
                return b[1].lane.screenPoint.distance - a[1].lane.screenPoint.distance
              }
              return (a[1].lane.position.x + a[1].lane.position.y + a[1].lane.position.z) - (b[1].lane.position.x + b[1].lane.position.y + b[1].lane.position.z)
            }
            )
            .map((kvp, index) => {
              const [_, { lane, defaultSpeedLimit }] = kvp
              let zIndex = MAX_DISPLAY_DISTANCE - Math.floor(lane.screenPoint.distance)

              while (usedZIndex.has(zIndex)) {
                zIndex++
              }
              usedZIndex.add(zIndex)

              return (
                <LanePage
                  zIndex={zIndex}
                  lane={lane}
                  defaultSpeedLimit={defaultSpeedLimit}
                  key={JSON.stringify(lane.position)}
                />
              );
            })
          : <></>
      }
    </>
  );
}