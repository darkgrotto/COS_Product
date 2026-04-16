import { useEffect, useState } from 'react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

interface UserRecord {
  id: string
  username: string
  displayName: string
  role: string
  state: string
}

interface CollectionEntry {
  cardIdentifier: string
  treatment: string
  quantity: number
  condition: string
  acquisitionPrice: number | null
  currentMarketValue: number | null
}

export function AdminContentUsers() {
  const [users, setUsers] = useState<UserRecord[]>([])
  const [selectedUserId, setSelectedUserId] = useState<string>('')
  const [collection, setCollection] = useState<CollectionEntry[]>([])
  const [usersLoading, setUsersLoading] = useState(true)
  const [collectionLoading, setCollectionLoading] = useState(false)
  const [usersError, setUsersError] = useState<string | null>(null)
  const [collectionError, setCollectionError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/users', { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<UserRecord[]>
      })
      .then(data => {
        const filtered = data.filter(u => u.state === 'Active' && u.role === 'GeneralUser')
        setUsers(filtered)
        setUsersLoading(false)
      })
      .catch(err => {
        setUsersError(err instanceof Error ? err.message : 'Failed to load users')
        setUsersLoading(false)
      })
  }, [])

  useEffect(() => {
    if (!selectedUserId) return
    setCollectionLoading(true)
    setCollectionError(null)
    setCollection([])
    fetch(`/api/collection?userId=${encodeURIComponent(selectedUserId)}`, { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<CollectionEntry[]>
      })
      .then(data => {
        setCollection(data)
        setCollectionLoading(false)
      })
      .catch(err => {
        setCollectionError(err instanceof Error ? err.message : 'Failed to load collection')
        setCollectionLoading(false)
      })
  }, [selectedUserId])

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <label className="text-sm font-medium">User</label>
        {usersLoading ? (
          <span className="text-sm text-muted-foreground">Loading users...</span>
        ) : usersError ? (
          <span className="text-sm text-destructive">Error: {usersError}</span>
        ) : (
          <Select value={selectedUserId} onValueChange={setSelectedUserId}>
            <SelectTrigger className="w-64">
              <SelectValue placeholder="Select a user..." />
            </SelectTrigger>
            <SelectContent>
              {users.map(u => (
                <SelectItem key={u.id} value={u.id}>
                  {u.displayName || u.username}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      </div>

      {selectedUserId && (
        <div className="overflow-auto">
          {collectionLoading && <p className="text-sm text-muted-foreground">Loading collection...</p>}
          {collectionError && <p className="text-sm text-destructive">Error: {collectionError}</p>}
          {!collectionLoading && !collectionError && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Card ID</TableHead>
                  <TableHead>Treatment</TableHead>
                  <TableHead>Qty</TableHead>
                  <TableHead>Condition</TableHead>
                  <TableHead>Acquisition Price</TableHead>
                  <TableHead>Market Value</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {collection.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="text-center text-muted-foreground">
                      No collection entries.
                    </TableCell>
                  </TableRow>
                ) : (
                  collection.map((entry, idx) => (
                    <TableRow key={idx}>
                      <TableCell className="font-mono text-xs">
                        {entry.cardIdentifier.toUpperCase()}
                      </TableCell>
                      <TableCell>{entry.treatment}</TableCell>
                      <TableCell>{entry.quantity}</TableCell>
                      <TableCell>{entry.condition}</TableCell>
                      <TableCell>
                        {entry.acquisitionPrice != null ? `$${entry.acquisitionPrice.toFixed(2)}` : '-'}
                      </TableCell>
                      <TableCell>
                        {entry.currentMarketValue != null ? `$${entry.currentMarketValue.toFixed(2)}` : '-'}
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          )}
        </div>
      )}
    </div>
  )
}
