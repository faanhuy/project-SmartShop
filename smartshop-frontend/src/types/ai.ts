export interface SemanticSearchResultDto {
  id: string;
  name: string;
  description: string;
  price: number;
  originalPrice: number;
  slug: string;
  imageUrl: string | null;
  categoryId: string;
  score: number;
}

export interface SemanticSearchRequest {
  query: string;
  topN?: number;
}

export interface GenerateDescriptionRequest {
  productName: string;
  categoryName: string;
}
