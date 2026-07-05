import {
  Box,
  Chip,
  LinearProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import type { ReactNode } from 'react'

export function DataPanel<T>({
  title,
  subtitle,
  rows,
  columns,
  empty,
  loading,
}: {
  title: string
  subtitle?: string
  rows: T[]
  empty?: string
  loading?: boolean
  columns: Array<{ key: string; label: string; render: (row: T) => ReactNode }>
}) {
  return (
    <Paper
      sx={{
        p: 3,
        '&:hover': {
          transform: 'translateY(-1px)',
        },
      }}
    >
      <Box sx={{ mb: 2 }}>
        <Typography variant="h5">{title}</Typography>
        {subtitle && (
          <Typography variant="body2" color="text.secondary">
            {subtitle}
          </Typography>
        )}
      </Box>
      {loading && <LinearProgress sx={{ mb: 1.5 }} />}
      <TableContainer>
        <Table>
          <TableHead>
            <TableRow>
              {columns.map((column) => (
                <TableCell key={column.key}>{column.label}</TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length}>
                  <Chip label="Loading data..." />
                </TableCell>
              </TableRow>
            ) : rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length}>
                  <Chip label={empty ?? 'No data yet'} />
                </TableCell>
              </TableRow>
            ) : (
              rows.map((row, index) => (
                <TableRow key={index} hover>
                  {columns.map((column) => (
                    <TableCell key={column.key}>{column.render(row)}</TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Paper>
  )
}
