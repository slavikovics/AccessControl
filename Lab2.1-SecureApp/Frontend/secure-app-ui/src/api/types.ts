export interface UserInfo {
  id: number;
  username: string;
}

export interface RecordItem {
  id: number;
  title: string;
  content: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ApiError {
  message: string;
}
