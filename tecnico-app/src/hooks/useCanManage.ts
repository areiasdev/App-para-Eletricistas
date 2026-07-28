import { useAuthStore } from '@/stores/authStore'

// Owner/Admin only: destructive actions (deleting clients/quotes/equipment/interventions),
// team management (invite/remove/role-change), and company-wide views (Reports, Audit Log).
// Technician/Commercial keep full read/write access to their own work otherwise.
export function useCanManage() {
  const role = useAuthStore((s) => s.user?.role)
  return role === 'Owner' || role === 'Admin'
}
