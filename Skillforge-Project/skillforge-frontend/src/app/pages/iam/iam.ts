import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { UserService } from '../../core/services/user.service';
import { User } from '../../core/models';
import { SharedModule } from '../../shared/shared.module';

@Component({
  selector: 'app-iam',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './iam.html',
})
export class IamComponent implements OnInit {
  private svc = inject(UserService);
  private fb = inject(FormBuilder);

  users = signal<User[]>([]);
  loading = signal(true);
  search = signal('');
  toast = signal('');
  toastType = signal<'success' | 'error'>('success');

  selectedUser = signal<User | null>(null);
  showEditModal = signal(false);
  editStatus = signal('true');
  updateLoading = signal(false);

  userToDelete = signal<User | null>(null);
  showDeleteModal = signal(false);
  deleteLoading = signal(false);

  showCreateModal = signal(false);
  createLoading = signal(false);

  createForm = this.fb.group({
    name:     ['', [Validators.required, Validators.maxLength(50)]],
    email:    ['', [Validators.required, Validators.email]],
    role:     ['Employee', Validators.required],
    phone:    ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  readonly creatableRoles = ['Employee', 'Trainer','Manager', 'HR'] as const;

  rolePermissions = [
    { role: 'Employee', cap: 'Enroll, attend, take assessments' },
    { role: 'Trainer', cap: 'Create courses, manage assessments' },
    { role: 'Manager', cap: 'Assign training, view team progress' },
    { role: 'HR', cap: 'Compliance, audits, reports' },
    { role: 'Admin', cap: 'Configure catalogs, users, workflows' },
  ];

  ngOnInit() {
    this.svc.getAll().subscribe({
      next: data => { this.users.set(data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  get filtered() {
    const q = this.search().toLowerCase();
    return q ? this.users().filter(u => u.userName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q)) : this.users();
  }

  openCreate() { this.showCreateModal.set(true); }
  closeCreate() {
    this.showCreateModal.set(false);
    this.createForm.reset({ role: 'Employee' });
  }

  submitCreate() {
    if (this.createForm.invalid) { this.createForm.markAllAsTouched(); return; }
    this.createLoading.set(true);
    const { name, email, role, phone, password } = this.createForm.value as any;
    this.svc.create({ name, email, role, phone, password }).subscribe({
      next: () => {
        this.closeCreate();
        this.showToast('User created successfully!', 'success');
        this.createLoading.set(false);
        this.svc.getAll().subscribe({ next: data => this.users.set(data), error: () => {} });
      },
      error: err => {
        const msg = err?.error?.message ?? 'Failed to create user.';
        this.showToast(msg, 'error');
        this.createLoading.set(false);
      }
    });
  }

  openEdit(user: User) {
    this.selectedUser.set(user);
    this.editStatus.set(user.status ? 'true' : 'false');
    this.showEditModal.set(true);
  }

  closeEdit() {
    this.showEditModal.set(false);
    this.selectedUser.set(null);
  }

  submitUpdate() {
    const user = this.selectedUser();
    if (!user) return;
    this.updateLoading.set(true);
    const newStatus = this.editStatus() === 'true';
    this.svc.updateStatus(user.userID, newStatus).subscribe({
      next: () => {
        this.users.update(list =>
          list.map(u => u.userID === user.userID ? { ...u, status: newStatus } : u)
        );
        this.closeEdit();
        this.updateLoading.set(false);
        this.showToast('User status updated successfully!', 'success');
      },
      error: err => {
        const msg = err?.error?.message ?? 'Failed to update user status.';
        this.showToast(msg, 'error');
        this.updateLoading.set(false);
      }
    });
  }

  openDelete(user: User) {
    this.userToDelete.set(user);
    this.showDeleteModal.set(true);
  }

  closeDelete() {
    this.showDeleteModal.set(false);
    this.userToDelete.set(null);
  }

  confirmDelete() {
    const user = this.userToDelete();
    if (!user) return;
    this.deleteLoading.set(true);
    this.svc.delete(user.userID).subscribe({
      next: () => {
        this.users.update(list => list.filter(u => u.userID !== user.userID));
        this.closeDelete();
        this.deleteLoading.set(false);
        this.showToast('User deleted successfully.', 'success');
      },
      error: err => {
        const msg = err?.error?.message
          ?? err?.error?.title
          ?? (typeof err?.error === 'string' ? err.error : null)
          ?? 'Failed to delete user.';
        this.showToast(msg, 'error');
        this.deleteLoading.set(false);
      }
    });
  }

  showToast(msg: string, type: 'success' | 'error') {
    this.toast.set(msg);
    this.toastType.set(type);
    setTimeout(() => this.toast.set(''), 3000);
  }
}
