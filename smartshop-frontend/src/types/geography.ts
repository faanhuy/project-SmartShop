export interface Province {
  id: number;
  name: string;
  code: string;
}

export interface Ward {
  id: number;
  provinceId: number;
  name: string;
  code: string;
}
