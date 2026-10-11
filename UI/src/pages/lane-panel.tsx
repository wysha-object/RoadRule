import BasePage from "components/base/base-page";
import LanePage from "components/lane-panel/lane-page";
import { UIToolModeContext } from "context";
import { requestScreenPoint, useGetAllLaneCmd, useGetAllLaneGroupCmd, useGetScreenPointCmd, useGetToolStateCmd } from "hooks/cmd";
import { useContext, useEffect, useMemo } from "react";
import { Lane, ToolState, UIToolMode } from "types";
import { MAX_DISPLAY_DISTANCE } from "utils";

export default function LanePanel() {
  const allLaneGroup = useGetAllLaneGroupCmd()
  const allLane = useGetAllLaneCmd()
  const toolState = useGetToolStateCmd()

  const screenPoints = useGetScreenPointCmd()

  const [mode] = useContext(UIToolModeContext)

  const lanes = toolState !== ToolState.Disabled ? (mode === UIToolMode.MasterLane ? allLaneGroup : allLane) : []

  useEffect(
    () => {
      requestScreenPoint("lane-page", lanes.map(item => item.lane.position));
    },
    [JSON.stringify(lanes)]
  )

  const usedZIndex = new Set<number>()

  return (
    <>
      {
        toolState !== ToolState.Disabled
        && lanes
          .reduce<{ lane: Lane, defaultSpeedLimit: number, screenPoint: any }[]>((prev, item) => {
            const screenPoint = screenPoints["lane-page"]
              .find(
                kvp => kvp.position.x === item.lane.position.x && kvp.position.y === item.lane.position.y && kvp.position.z === item.lane.position.z
              )?.screenPoint
            if (!screenPoint) {
              return prev;
            }
            const newItem = {
              ...item,
              screenPoint: screenPoint
            }
            prev.push(newItem);
            return prev;
          }, [])
          .sort((a, b) => {
            if (a.screenPoint.distance !== b.screenPoint.distance) {
              return b.screenPoint.distance - a.screenPoint.distance
            }
            return (a.lane.position.x + a.lane.position.y + a.lane.position.z) - (b.lane.position.x + b.lane.position.y + b.lane.position.z)
          }
          )
          .map((item) => {
            let zIndex = MAX_DISPLAY_DISTANCE - Math.floor(item.screenPoint.distance)

            while (usedZIndex.has(zIndex)) {
              zIndex++
            }
            usedZIndex.add(zIndex)

            return (
              <LanePage
                style={{
                  top: item.screenPoint.top,
                  left: item.screenPoint.left,
                  opacity: item.screenPoint.distance > 500 ? 1 - ((item.screenPoint.distance - 500) / (MAX_DISPLAY_DISTANCE - 500)) : 1,
                  transformOrigin: 'top left',
                  transform: `translate(-10rem, -10rem) scale(${item.screenPoint.scale}) translate(-50%, -50%)`,
                  zIndex: zIndex
                }}
                lane={item.lane}
                defaultSpeedLimit={item.defaultSpeedLimit}
                key={JSON.stringify(item.lane.position)}
              />
            );
          })
      }
    </>
  );
}