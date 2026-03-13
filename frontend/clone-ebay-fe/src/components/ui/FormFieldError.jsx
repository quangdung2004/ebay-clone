import './FormFieldError.css';

/**
 * Tiny inline error message displayed below a form field.
 * Renders nothing if error is falsy.
 */
const FormFieldError = ({ error }) => {
    if (!error) return null;
    return <span className="form-field-error" role="alert">{error}</span>;
};

export default FormFieldError;
