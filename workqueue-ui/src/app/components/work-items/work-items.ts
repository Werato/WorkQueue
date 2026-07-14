import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth';
import { WorkItemService, WorkItem } from '../../services/work-item';

@Component({
  selector: 'app-work-items',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './work-items.html',
  styleUrls: ['./work-items.css']
})

export class WorkItemsComponent implements OnInit {
	private authService = inject(AuthService);
	private workItemService = inject(WorkItemService);
	private router = inject(Router);
	private fb = inject(FormBuilder);
	private cdr = inject(ChangeDetectorRef);

	userName = '';
	orgName = '';
	workItems: WorkItem[] = [];

	userRole = '';
	currentUserId = '';
	orgUsers: any[] = [];
	
	editingTask: any = null; 
	comments: any[] = [];
	newCommentText = '';
	
	selectedStatus: number | null = null;
	selectedPriority: number | null = null;
	showCreateForm = false;
	taskForm!: FormGroup;
	
	currentPage = 1;
	pageSize = 5;
	totalCount = 0;
	searchQuery = '';
	sortBy = 'createdDate';
	
	ngOnInit() {
		if (typeof window !== 'undefined' && typeof localStorage !== 'undefined') {
			
		this.taskForm = this.fb.group({
			title: ['', Validators.required],
			description: [''],
			priority: [1],
			status: [0],
			assigneeId: [null]
		});
			
			const profileStr = localStorage.getItem('user_profile');
			if (profileStr) {
				const profile = JSON.parse(profileStr);
				this.userName = profile.name;
				this.orgName = profile.organizationName;
				
				this.userRole = profile.role;
				this.currentUserId = profile.id;
				if (profile.role === 0 || profile.role === 'Manager') {
					this.userRole = 'Manager';
				} else if (profile.role === 1 || profile.role === 'Member') {
					this.userRole = 'Member';
				} else {
					this.userRole = profile.role;
				}
			}
		}

		this.loadWorkItems();
		this.loadUsers();
	}

	loadWorkItems() {
	  this.workItemService.getWorkItems(
		this.currentPage,
		this.pageSize,
		this.searchQuery,
		this.selectedStatus ?? undefined,
		this.selectedPriority ?? undefined,
		this.sortBy
	  ).subscribe({
		next: (response) => {
		  this.workItems = response.items;
		  this.totalCount = response.totalCount;
		  this.cdr.detectChanges(); 
		},
		error: (err) => console.error(err)
	  });
	}
	
	onSearch() {
		this.currentPage = 1; 
		this.loadWorkItems();
	}

	nextPage() {
		if (this.currentPage * this.pageSize < this.totalCount) {
			this.currentPage++;
			this.loadWorkItems();
		}
	}

	prevPage() {
		if (this.currentPage > 1) {
			this.currentPage--;
			this.loadWorkItems();
		}
	}
	
	loadUsers() {
		this.workItemService.getUsers().subscribe({
			next: (users : any[]) => this.orgUsers = users,
			error: (err: any) => console.error('Error: ', err)
		});
	}
openCreateForm() {
		this.taskForm.reset({ priority: 1, status: 0 });
		this.editingTask = null;
		this.showCreateForm = true;
	}

	openEditForm(task: WorkItem) {
		this.editingTask = task;
		this.taskForm.patchValue({
			title: task.title,
			description: task.description,
			priority: task.priority,
			status: task.status,
			assigneeId: task.assigneeId
		});
		this.showCreateForm = true;
		this.loadComments(task.id);
	}

	closeForm() {
		this.showCreateForm = false;
		this.editingTask = null;
	}

	saveTask() {
		if (this.taskForm.invalid) return;

		const formData = this.taskForm.value;

		if (this.editingTask) {
			const updatedItem = { ...this.editingTask, ...formData };
			this.workItemService.updateWorkItem(this.editingTask.id, updatedItem).subscribe({
				next: () => {
					this.closeForm();
					this.loadWorkItems();
				},
				error: (err: any) => console.error('Error: ', err)
			});
		} else {
			this.workItemService.createWorkItem(formData).subscribe({
				next: () => {
					this.closeForm();
					this.loadWorkItems();
				},
				error: (err: any) => console.error('Error: ', err)
			});
		}
	}

	canEditStatus(): boolean {
	if (this.userRole === 'Manager') return true;
		return this.userRole === 'Member' && this.editingTask.assigneeId === this.currentUserId;
	}

onFilterChange() {
  this.currentPage = 1;
  this.loadWorkItems();
}

	logout() {
		this.authService.logout();
		this.router.navigate(['/login']);
	}
	
	getStatusName(status: number): string {
		const map = ['To Do', 'In Progress', 'Done'];
		return map[status] || 'Unknown';
	}

	getPriorityName(priority: number): string {
		const map = ['Low', 'Normal', 'High'];
		return map[priority] || 'Unknown';
	}
	loadComments(workItemId: string) {
    this.workItemService.getComments(workItemId).subscribe({
      next: (data: any[]) => this.comments = data,
      error: (err: any) => console.error('Error: ', err)
    });
  }

  submitComment() {
    if (!this.newCommentText.trim()) return;

    this.workItemService.addComment(this.editingTask.id, this.newCommentText).subscribe({
      next: (newComment: any) => {
        this.comments.push(newComment); // Добавляем свежий коммент в конец списка чата
        this.newCommentText = ''; // Очищаем текстовое поле
      },
      error: (err: any) => console.error('Error: ', err)
    });
  }
  
  changeStatus(id: string, newStatus: number) {
  this.workItemService.transitionWorkItem(id, newStatus).subscribe({
    next: () => {
      this.loadWorkItems();
    },
    error: (err) => {
      alert(err.error || 'Failed to change status');
    }
  });
}
  
}