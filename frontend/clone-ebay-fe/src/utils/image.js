export const getPlaceholderImage = () => {
  return "https://via.placeholder.com/300x300?text=No+Image";
};

export const normalizeProductImageUrl = (value) => {
  if (!value) return getPlaceholderImage();

  if (typeof value !== "string") return getPlaceholderImage();

  const trimmed = value.trim();
  if (!trimmed) return getPlaceholderImage();

  if (
    trimmed.startsWith("http://") ||
    trimmed.startsWith("https://") ||
    trimmed.startsWith("data:image")
  ) {
    return trimmed;
  }

  return trimmed;
};