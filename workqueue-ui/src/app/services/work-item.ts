import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface WorkItem {
  id: string;
  title: string;
  description: string;
  status: number; // 0: ToDo, 1: InProgress, 2: Done
  priority: number; // 0: Low, 1: Normal, 2: High
  dueDate?: string;
  assigneeId?: string;
  assigneeName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class WorkItemService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7122/api/workitems';

  createWorkItem(item: any): Observable<any> {
    return this.http.post(this.apiUrl, item);
  }

getWorkItems(page: number, pageSize: number, search?: string, status?: number, priority?: number, sortBy?: string): Observable<any> {
  let params = new HttpParams()
    .set('page', page.toString())
    .set('pageSize', pageSize.toString());

  if (search) params = params.set('search', search);
  if (status !== null && status !== undefined) params = params.set('status', status.toString());
  if (priority !== null && priority !== undefined) params = params.set('priority', priority.toString());
  if (sortBy) params = params.set('sortBy', sortBy);

  return this.http.get<any>(this.apiUrl, { params });
}

  getUsers(): Observable<any[]> {
    return this.http.get<any[]>('https://localhost:7122/api/users'); 
  }
  
transitionWorkItem(id: string, newStatus: number): Observable<any> {
  return this.http.post<any>(`${this.apiUrl}/${id}/transition`, { newStatus });
}
updateWorkItem(id: string, item: any): Observable<any> {
  return this.http.patch<any>(`${this.apiUrl}/${id}`, item);
}
  getComments(workItemId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${workItemId}/comments`);
  }

  addComment(workItemId: string, text: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${workItemId}/comments`, { text });
  }
}