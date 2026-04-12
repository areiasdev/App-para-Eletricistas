import { z } from 'zod'

export const registerSchema = z
  .object({
    fullName: z
      .string()
      .min(2, 'O nome deve ter pelo menos 2 caracteres.')
      .max(200),
    email: z.string().email('O email não é válido.'),
    password: z
      .string()
      .min(8, 'A password deve ter pelo menos 8 caracteres.')
      .regex(/[A-Z]/, 'A password deve ter pelo menos uma letra maiúscula.')
      .regex(/[0-9]/, 'A password deve ter pelo menos um número.'),
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'As passwords não coincidem.',
    path: ['confirmPassword'],
  })

export const loginSchema = z.object({
  email: z.string().email('O email não é válido.'),
  password: z.string().min(1, 'A password é obrigatória.'),
})

export type RegisterFormValues = z.infer<typeof registerSchema>
export type LoginFormValues = z.infer<typeof loginSchema>
