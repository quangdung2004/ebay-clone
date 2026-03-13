export const formatCurrency = (amount) => {
    if (amount == null) return '';
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND',
    }).format(amount);
};

export const formatDateTime = (dateString) => {
    if (!dateString) return '';
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('vi-VN', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    }).format(date);
};

export const getStatusLabel = (status) => {
    switch (status) {
        case 'ACTIVE':
            return { text: 'Active', color: 'success' };
        case 'INACTIVE':
            return { text: 'Inactive', color: 'secondary' };
        case 'OUT_OF_STOCK':
            return { text: 'Out of Stock', color: 'danger' };
        case 'ENDED':
            return { text: 'Ended', color: 'dark' };
        case 'SOLD':
            return { text: 'Sold', color: 'success-dark' };
        default:
            return { text: status, color: 'primary' };
    }
};

export const getConditionLabel = (condition) => {
    switch (condition) {
        case 'NEW':
            return { text: 'New', color: 'primary' };
        case 'USED':
            return { text: 'Used', color: 'warning' };
        case 'REFURBISHED':
            return { text: 'Refurbished', color: 'info' };
        case 'OPEN_BOX':
            return { text: 'Open Box', color: 'secondary' };
        case 'FOR_PARTS':
            return { text: 'For Parts', color: 'danger' };
        default:
            return { text: condition, color: 'secondary' };
    }
};

export const getPlaceholderImage = () => {
    return 'https://dummyimage.com/300x300/e5e7eb/6b7280.png&text=No+Image';
};

export const normalizeProductImageUrl = (url) => {
    if (!url) return getPlaceholderImage();
    try {
        const parsed = new URL(url);
        // If it's pointing to local backend, force relative so Vite's proxy handles it and prevents CERT error
        if (parsed.hostname === 'localhost' || parsed.hostname === '127.0.0.1') {
            return parsed.pathname + parsed.search;
        }
        return url;
    } catch (e) {
        // If parsing fails (likely already a relative path), return as is
        return url;
    }
};
