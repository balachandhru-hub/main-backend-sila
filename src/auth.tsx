import React, { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { getClaims, login as loginRequest, logout as logoutRequest, type TokenClaims } from './api'

export type AppRole = 'buyer' | 'supplier' | 'platform'

export interface Session {
  userId: string
  personId: string
  organizationId: string
  roleId: string
  role: AppRole
  roleLabel: string
  buyerId?: string
  supplierId?: string
}

const ROLE_LABELS: Record<string, { role: AppRole; label: string }> = {
  '5a72f81e-a2c5-4f4a-bd55-6376c3c9ed73': { role: 'buyer', label: 'Buyer' },
  '937aab61-b505-4e1c-a5a3-cd63e29c6db9': { role: 'supplier', label: 'Supplier' },
  'c95f5a1b-4aec-4647-9328-895a58193ec4': { role: 'platform', label: 'Buyer administrator' },
  '735bb267-fec0-489f-8249-d3d65b3857ea': { role: 'platform', label: 'Supplier administrator' },
  '61eb9b97-1fca-4beb-beb8-dc4b379cfa3a': { role: 'platform', label: 'Buyer network admin' },
  '22067509-af24-48f8-a7e9-416a0b6a439b': { role: 'platform', label: 'Supplier network admin' },
  '113d8ead-40c2-425a-bc60-5989e6cdabca': { role: 'platform', label: 'Platform administrator' },
}

function toSession(claims: TokenClaims): Session {
  const known = ROLE_LABELS[claims.roleId]
  const buyerId = claims.buyerId || undefined
  const supplierId = claims.supplierId || undefined
  const resolved = known ?? { role: 'platform' as AppRole, label: 'SILA user' }
  return {
    userId: claims.userId,
    personId: claims.personId,
    organizationId: claims.organizationId,
    roleId: claims.roleId,
    role: resolved.role,
    roleLabel: resolved.label,
    buyerId,
    supplierId,
  }
}

interface AuthValue {
  session: Session | null
  ready: boolean
  signIn: (userName: string, password: string) => Promise<Session>
  signOut: () => Promise<void>
}

const AuthContext = createContext<AuthValue | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<Session | null>(null)
  const [ready, setReady] = useState(false)

  useEffect(() => {
    let active = true
    getClaims()
      .then((claims) => {
        if (active && claims?.userId && claims.roleId) setSession(toSession(claims))
      })
      .catch(() => undefined)
      .finally(() => {
        if (active) setReady(true)
      })
    const onExpired = () => setSession(null)
    window.addEventListener('session:expired', onExpired)
    return () => {
      active = false
      window.removeEventListener('session:expired', onExpired)
    }
  }, [])

  const value = useMemo<AuthValue>(() => ({
    session,
    ready,
    signIn: async (userName, password) => {
      await loginRequest(userName, password)
      const claims = await getClaims()
      if (!claims?.roleId) throw new Error('Signed in, but the account role could not be read.')
      const next = toSession(claims)
      setSession(next)
      return next
    },
    signOut: async () => {
      await logoutRequest()
      setSession(null)
    },
  }), [session, ready])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used inside AuthProvider')
  return value
}
