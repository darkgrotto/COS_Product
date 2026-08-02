import * as React from "react"
import { cn } from "@/lib/utils"

// Two responsive behaviours, both centralised here:
//  - "default": on phones the table keeps its natural width and scrolls
//    horizontally (max-sm:min-w-max) instead of crushing columns; desktop
//    is unchanged.
//  - "card": below sm the table reflows so each row becomes a titled card.
//    Cells opt in by passing a `label`, which is shown as the field name.
type TableVariant = "default" | "card"
const TableVariantContext = React.createContext<TableVariant>("default")

const Table = React.forwardRef<
  HTMLTableElement,
  React.HTMLAttributes<HTMLTableElement> & { variant?: TableVariant }
>(({ className, variant = "default", ...props }, ref) => (
  <TableVariantContext.Provider value={variant}>
    <div className="relative w-full overflow-x-auto overscroll-x-contain">
      <table
        ref={ref}
        className={cn(
          "w-full caption-bottom text-sm",
          variant === "default" && "max-sm:min-w-max",
          variant === "card" && "max-sm:block",
          className
        )}
        {...props}
      />
    </div>
  </TableVariantContext.Provider>
))
Table.displayName = "Table"

const TableHeader = React.forwardRef<HTMLTableSectionElement, React.HTMLAttributes<HTMLTableSectionElement>>(
  ({ className, ...props }, ref) => {
    const variant = React.useContext(TableVariantContext)
    return (
      <thead
        ref={ref}
        className={cn("[&_tr]:border-b", variant === "card" && "max-sm:hidden", className)}
        {...props}
      />
    )
  }
)
TableHeader.displayName = "TableHeader"

const TableBody = React.forwardRef<HTMLTableSectionElement, React.HTMLAttributes<HTMLTableSectionElement>>(
  ({ className, ...props }, ref) => {
    const variant = React.useContext(TableVariantContext)
    return (
      <tbody
        ref={ref}
        className={cn("[&_tr:last-child]:border-0", variant === "card" && "max-sm:block", className)}
        {...props}
      />
    )
  }
)
TableBody.displayName = "TableBody"

const TableFooter = React.forwardRef<HTMLTableSectionElement, React.HTMLAttributes<HTMLTableSectionElement>>(
  ({ className, ...props }, ref) => {
    const variant = React.useContext(TableVariantContext)
    return (
      <tfoot
        ref={ref}
        className={cn(
          "border-t bg-muted/50 font-medium [&>tr]:last:border-b-0",
          variant === "card" && "max-sm:block max-sm:bg-transparent",
          className
        )}
        {...props}
      />
    )
  }
)
TableFooter.displayName = "TableFooter"

const TableRow = React.forwardRef<HTMLTableRowElement, React.HTMLAttributes<HTMLTableRowElement>>(
  ({ className, ...props }, ref) => {
    const variant = React.useContext(TableVariantContext)
    return (
      <tr
        ref={ref}
        className={cn(
          "border-b transition-colors hover:bg-muted/50 data-[state=selected]:bg-muted",
          variant === "card" &&
            "max-sm:block max-sm:mb-3 max-sm:rounded-lg max-sm:border max-sm:p-3 max-sm:shadow-sm max-sm:hover:bg-transparent",
          className
        )}
        {...props}
      />
    )
  }
)
TableRow.displayName = "TableRow"

const TableHead = React.forwardRef<HTMLTableCellElement, React.ThHTMLAttributes<HTMLTableCellElement>>(
  ({ className, ...props }, ref) => (
    <th
      ref={ref}
      className={cn(
        "h-12 px-4 text-left align-middle font-medium text-muted-foreground [&:has([role=checkbox])]:pr-0",
        className
      )}
      {...props}
    />
  )
)
TableHead.displayName = "TableHead"

// In card mode, `label` is rendered as the field name for each cell so a
// stacked card row reads "Field: value". The label is hidden at sm and up.
const TableCell = React.forwardRef<
  HTMLTableCellElement,
  React.TdHTMLAttributes<HTMLTableCellElement> & { label?: string }
>(({ className, label, children, ...props }, ref) => {
  const variant = React.useContext(TableVariantContext)
  return (
    <td
      ref={ref}
      className={cn(
        "p-4 align-middle [&:has([role=checkbox])]:pr-0",
        variant === "card" &&
          "max-sm:flex max-sm:items-center max-sm:justify-between max-sm:gap-3 max-sm:border-0 max-sm:px-0 max-sm:py-1.5 max-sm:text-right",
        className
      )}
      {...props}
    >
      {variant === "card" && label != null && (
        <span className="hidden max-sm:block text-xs font-medium uppercase tracking-wide text-muted-foreground text-left">
          {label}
        </span>
      )}
      {children}
    </td>
  )
})
TableCell.displayName = "TableCell"

const TableCaption = React.forwardRef<HTMLTableCaptionElement, React.HTMLAttributes<HTMLTableCaptionElement>>(
  ({ className, ...props }, ref) => (
    <caption ref={ref} className={cn("mt-4 text-sm text-muted-foreground", className)} {...props} />
  )
)
TableCaption.displayName = "TableCaption"

export { Table, TableHeader, TableBody, TableFooter, TableHead, TableRow, TableCell, TableCaption }
