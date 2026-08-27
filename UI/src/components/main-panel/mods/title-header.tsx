import { useTranslate } from "hooks/translate";

export default function TitleHeader() {
  const { t } = useTranslate();
  return (
    <div style={{
      fontSize: '1.1em',
      display: 'flex',
      alignItems: 'center',
      width: '100%',
      height: '100%',
      padding: '0.2em 1em 0',
    }}>
      {t('RoadRule')}
    </div>
  )
}