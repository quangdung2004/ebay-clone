import { formatDateTime } from '../../utils/productUtils';
import './TrackingTimeline.css';

const TrackingTimeline = ({ events }) => {
  if (!events || events.length === 0) {
    return <div className="tracking-timeline-empty">No tracking checkpoints available yet.</div>;
  }

  return (
    <div className="tracking-timeline-container">
      <ul className="timeline">
        {events.map((evt, idx) => (
          <li key={idx} className={`timeline-item ${idx === 0 ? 'latest' : ''}`}>
            <div className="timeline-marker"></div>
            <div className="timeline-content">
              <div className="timeline-time">
                {evt.eventTime ? formatDateTime(evt.eventTime) : 'N/A'}
              </div>
              <div className="timeline-desc-container">
                <p className="timeline-checkpoint">
                   <strong>{evt.description || evt.mainStatus || 'Status Update'}</strong>
                </p>
                <p className="timeline-location">
                    📍 {evt.location || 'Destination'}
                </p>
                {(evt.mainStatus || evt.subStatus) && (
                   <p className="timeline-status-badge">
                      {evt.mainStatus} {evt.subStatus ? ` - ${evt.subStatus}` : ''}
                   </p>
                )}
              </div>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
};

export default TrackingTimeline;
