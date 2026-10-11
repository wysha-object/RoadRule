import type { Camera, Position, ScreenPoint } from 'types'

export const MAX_DISPLAY_DISTANCE = 2000
const SCALE_REFERENCE_DISTANCE = 100

export function getScreenPoint(position: Position, camera: Camera | null): ScreenPoint | null {
  if (camera === null) {
    return null
  }

  // Matrices are row-major; Unity camera space looks down the negative Z axis.
  const view = camera.worldToCameraMatrix
  const x = view[0] * position.x + view[1] * position.y + view[2] * position.z + view[3]
  const y = view[4] * position.x + view[5] * position.y + view[6] * position.z + view[7]
  const z = view[8] * position.x + view[9] * position.y + view[10] * position.z + view[11]
  const distance = -z
  if (distance < camera.nearClipPlane || distance > MAX_DISPLAY_DISTANCE) {
    return null
  }

  const projection = camera.projectionMatrix
  const clipX = projection[0] * x + projection[1] * y + projection[2] * z + projection[3]
  const clipY = projection[4] * x + projection[5] * y + projection[6] * z + projection[7]
  const clipW = projection[12] * x + projection[13] * y + projection[14] * z + projection[15]
  const viewport = camera.pixelRect
  const left = viewport.x + (clipX / clipW + 1) * viewport.width / 2
  const bottom = viewport.y + (clipY / clipW + 1) * viewport.height / 2
  if (left < 0 || left > camera.screenWidth || bottom < 0 || bottom > camera.screenHeight) {
    return null
  }

  const referenceW = projection[14] * -SCALE_REFERENCE_DISTANCE + projection[15]
  const pointW = projection[14] * -Math.max(distance, camera.nearClipPlane) + projection[15]
  return {
    top: camera.screenHeight - bottom,
    left,
    distance,
    scale: referenceW / pointW,
  }
}
