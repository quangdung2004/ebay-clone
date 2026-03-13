import { useNavigate } from 'react-router-dom';
import { createStore } from '../api/storeApi';
import { useToast } from '../components/Toast';
import StoreForm from '../components/store/StoreForm';
import './CreateStorePage.css';

const CreateStorePage = () => {
    const navigate = useNavigate();
    const { showSuccess } = useToast();

    const handleCreate = async (payload) => {
        // StoreForm handles try/catch/loading internally
        // If this throws, StoreForm catches it and shows the error
        await createStore(payload);
        showSuccess('Store created successfully!');
        navigate('/sell/new');
    };

    return (
        <div className="create-store-page">
            <div className="csp-card">
                <h1 className="csp-title">Create Your Store</h1>
                <p className="csp-subtitle">
                    Set up your store to start listing and selling products.
                </p>

                <StoreForm
                    onSubmit={handleCreate}
                    submitLabel="Create Store"
                />
            </div>
        </div>
    );
};

export default CreateStorePage;
