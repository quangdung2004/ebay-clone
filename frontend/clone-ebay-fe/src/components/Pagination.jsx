import './Pagination.css';

const Pagination = ({ page, pageSize, total, onPageChange }) => {
    const totalPages = Math.max(1, Math.ceil(total / pageSize));

    if (totalPages <= 1) return null;

    const pages = [];
    const maxVisible = 5;
    let start = Math.max(1, page - Math.floor(maxVisible / 2));
    let end = Math.min(totalPages, start + maxVisible - 1);
    if (end - start + 1 < maxVisible) {
        start = Math.max(1, end - maxVisible + 1);
    }

    for (let i = start; i <= end; i++) pages.push(i);

    return (
        <div className="pagination" role="navigation" aria-label="Pagination">
            <button
                className="pagination__btn"
                disabled={page <= 1}
                onClick={() => onPageChange(page - 1)}
            >
                ‹ Prev
            </button>

            {start > 1 && (
                <>
                    <button className="pagination__num" onClick={() => onPageChange(1)}>1</button>
                    {start > 2 && <span className="pagination__dots">…</span>}
                </>
            )}

            {pages.map((p) => (
                <button
                    key={p}
                    className={`pagination__num ${p === page ? 'pagination__num--active' : ''}`}
                    onClick={() => onPageChange(p)}
                >
                    {p}
                </button>
            ))}

            {end < totalPages && (
                <>
                    {end < totalPages - 1 && <span className="pagination__dots">…</span>}
                    <button className="pagination__num" onClick={() => onPageChange(totalPages)}>
                        {totalPages}
                    </button>
                </>
            )}

            <button
                className="pagination__btn"
                disabled={page >= totalPages}
                onClick={() => onPageChange(page + 1)}
            >
                Next ›
            </button>

            <span className="pagination__info">
                Page {page} of {totalPages} ({total} items)
            </span>
        </div>
    );
};

export default Pagination;
