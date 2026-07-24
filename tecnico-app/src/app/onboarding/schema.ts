import { z } from 'zod'
import { validateNif } from '@/lib/utils/formatters'

// Step 1 of the onboarding wizard ("Company info"). Only companyName is required —
// NIF and phone are optional but, when present, validated the same way as everywhere
// else in the app (ClientForm.tsx, perfil/page.tsx): 9-digit format + checksum.
export const companyInfoSchema = z.object({
  companyName: z.string().min(1, 'O nome da empresa é obrigatório.').max(200),
  nif: z
    .string()
    .regex(/^\d{9}$/, 'O NIF deve ter 9 dígitos.')
    .refine((v) => validateNif(v), 'NIF inválido.')
    .optional()
    .or(z.literal('')),
  phone: z.string().max(20).optional().or(z.literal('')),
})

export type CompanyInfoFormValues = z.infer<typeof companyInfoSchema>
