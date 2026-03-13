import { useRef } from 'react';
import './ImageUploader.css';

const ImageUploader = ({ files, onChange, loading = false }) => {
    const inputRef = useRef(null);

    const handleFiles = (e) => {
        const newFiles = Array.from(e.target.files);
        if (newFiles.length) onChange([...files, ...newFiles]);
        e.target.value = ''; // allow re-selecting same file
    };

    const removeFile = (index) => {
        onChange(files.filter((_, i) => i !== index));
    };

    return (
        <div className="image-uploader">
            <input
                ref={inputRef}
                type="file"
                multiple
                accept="image/*"
                onChange={handleFiles}
                className="image-uploader__input"
                disabled={loading}
            />
            <button
                type="button"
                className="image-uploader__btn"
                onClick={() => inputRef.current?.click()}
                disabled={loading}
            >
                {loading ? 'Uploading…' : '📷 Choose Images'}
            </button>

            {files.length > 0 && (
                <div className="image-uploader__preview">
                    {files.map((file, i) => (
                        <div key={i} className="image-uploader__thumb">
                            <img src={URL.createObjectURL(file)} alt={file.name} />
                            <button
                                type="button"
                                className="image-uploader__remove"
                                onClick={() => removeFile(i)}
                                aria-label="Remove"
                            >
                                ×
                            </button>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};

export default ImageUploader;
