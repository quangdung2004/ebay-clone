import { Link } from 'react-router-dom';
import './StoreEmptyState.css';

/**
 * Shown when the user has no store yet.
 *
 * Props:
 *   title    – heading text (default "Start Selling")
 *   message  – body text
 */
const StoreEmptyState = ({
    title = 'Start Selling',
    message = 'To list products, you need to create your store first.',
}) => {
    return (
        <div className="store-empty-state">
            <div className="store-empty-icon">🏪</div>
            <h2>{title}</h2>
            <p>{message}</p>
            <Link to="/stores/create" className="store-empty-cta">
                Create Store
            </Link>
        </div>
    );
};

export default StoreEmptyState;
