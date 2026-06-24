import { useState } from 'react'
import { CenterPanel } from './components/CenterPanel'
import { Header } from './components/Header'
import { LeftPanel } from './components/LeftPanel'
import { RightPanel } from './components/RightPanel'

type PreviewTab = 'image' | 'model'

function App() {
  const [activeTab, setActiveTab] = useState<PreviewTab>('image')

  return (
    <div className="min-h-screen bg-[#0d1117] text-[#f0f6fc]">
      <div className="mx-auto flex min-h-screen max-w-[1720px] flex-col">
        <Header />
        <main className="flex-1 px-4 pb-4 pt-24">
          <div className="grid h-[calc(100vh-6.5rem)] min-h-[760px] grid-cols-1 gap-4 xl:grid-cols-[300px_minmax(0,1fr)_320px]">
            <LeftPanel />
            <CenterPanel activeTab={activeTab} onTabChange={setActiveTab} />
            <RightPanel />
          </div>
        </main>
      </div>
    </div>
  )
}

export default App
