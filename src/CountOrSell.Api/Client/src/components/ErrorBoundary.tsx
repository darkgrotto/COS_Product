import { Component, ErrorInfo, ReactNode } from 'react'
import { AlertTriangle } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface Props {
  children: ReactNode
}

interface State {
  error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // Surface to the console for inspection. The server-side log forwarder
    // already captures unhandled rejections from the browser via window error
    // events, so we only need a visible local trace here.
    console.error('Unhandled error:', error, info.componentStack)
  }

  handleReset = () => {
    this.setState({ error: null })
  }

  handleReload = () => {
    window.location.reload()
  }

  render() {
    if (this.state.error) {
      return (
        <div className="min-h-[60vh] flex items-center justify-center p-6">
          <div className="max-w-md w-full rounded-lg border bg-card p-6 space-y-4">
            <div className="flex items-center gap-2 text-destructive">
              <AlertTriangle className="h-5 w-5" />
              <h2 className="text-lg font-semibold">Something went wrong</h2>
            </div>
            <p className="text-sm text-muted-foreground">
              The page hit an unexpected error. You can try again, or reload
              the app if the problem persists.
            </p>
            <details className="text-xs text-muted-foreground rounded-md border bg-muted/30 p-2">
              <summary className="cursor-pointer">Error detail</summary>
              <pre className="mt-2 whitespace-pre-wrap break-words font-mono">
                {this.state.error.message}
              </pre>
            </details>
            <div className="flex gap-2">
              <Button variant="outline" onClick={this.handleReset}>Try again</Button>
              <Button onClick={this.handleReload}>Reload app</Button>
            </div>
          </div>
        </div>
      )
    }
    return this.props.children
  }
}
