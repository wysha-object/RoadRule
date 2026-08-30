import DeleteSvg from 'assets/images/delete.svg'
import BasePage from 'components/base/base-page'
import { Button, Scrollable } from 'cs2/ui'
import {
  lookAt,
  setToolStateCmd,
  useGetSelectedEdgeEntityCmd,
  useGetToolStateCmd,
} from 'hooks/cmd'
import { useTranslate } from 'hooks/translate'
import TitleHeader from './mods/title-header'
import { ToolState } from 'types'

export default function LeftPage() {
  const { t } = useTranslate()
  const slectedEdges = useGetSelectedEdgeEntityCmd()

  return (
    <BasePage
      style={{
        width: '10em',
        maxHeight: '56em',
      }}
      header={<TitleHeader />}
      footer={<Footer />}
    >
      <div
        style={{
          backgroundColor: 'rgba(19, 28, 48, 0.75)',
        }}
      >
        <Scrollable>
          {slectedEdges.map((edge, index) => (
            <div
              key={edge.edgeEntity.index}
              className='row-with-hover-effect'
              onClick={() => {
                lookAt(edge.position.x, edge.position.y, edge.position.z, 200)
              }}
            >
              {t('MainPanel.EdgeEntity', {
                'entity-index': `${edge.edgeEntity.index}`,
              })}
            </div>
          ))}
        </Scrollable>
      </div>
    </BasePage>
  )
}

function Footer() {
  const { t } = useTranslate()
  const slectedEdgeEntities = useGetSelectedEdgeEntityCmd()
  const toolState = useGetToolStateCmd()

  return (
    <>
      {[ToolState.Choosed].includes(toolState) && (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            width: '100%',
            height: '100%',
            padding: '0.2em 1em 0',
          }}
        >
          <div
            style={{
              flex: '1',
            }}
          >
            {t('MainPanel.SelectedEdgeCount', {
              count: `${slectedEdgeEntities.length}`,
            })}
          </div>
          <Button
            variant='round'
            onClick={() => {
              setToolStateCmd(ToolState.Choosing)
            }}
          >
            <DeleteSvg />
          </Button>
        </div>
      )}
    </>
  )
}
