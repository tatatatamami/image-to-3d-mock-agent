const promptText = `未来的な小型ロボットを作ってください。
丸みのあるボディで、白と黒のメタル素材、
青いライトがアクセントのデザイン。
愛らしくて、相棒のような雰囲気にしてください。
SF感のある清潔なスタジオ背景でお願いします。`

const quickIdeas = ['もっとリアル', 'かわいく', 'メカ風', 'ファンタジー']

function SectionTitle({ children, info }: { children: string; info?: boolean }) {
  return (
    <div className="mb-3 flex items-center justify-between">
      <h3 className="text-sm font-medium text-[#f0f6fc]">{children}</h3>
      {info ? <span className="text-xs text-[#8b949e]">ℹ</span> : null}
    </div>
  )
}

function UploadedThumb() {
  return (
    <div className="relative flex aspect-square flex-1 items-center justify-center overflow-hidden rounded-2xl border border-white/10 bg-gradient-to-br from-[#1f2631] to-[#121821]">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(255,255,255,0.12),transparent_45%)]" />
      <div className="relative flex h-20 w-16 items-center justify-center rounded-[1.75rem] border border-cyan-400/25 bg-white/90 shadow-[0_16px_40px_rgba(0,0,0,0.35)]">
        <div className="absolute -top-3 h-8 w-8 rounded-full border border-slate-300 bg-white" />
        <div className="absolute bottom-3 h-8 w-10 rounded-[1rem] border border-slate-300 bg-gradient-to-b from-slate-50 to-slate-200" />
        <div className="absolute left-2 top-8 h-5 w-2 rounded-full bg-slate-900" />
        <div className="absolute right-2 top-8 h-5 w-2 rounded-full bg-slate-900" />
        <div className="absolute left-1/2 top-8 h-2 w-2 -translate-x-1/2 rounded-full bg-cyan-400 shadow-[0_0_10px_rgba(34,211,238,0.8)]" />
      </div>
    </div>
  )
}

function EmptyUploadSlot() {
  return (
    <button
      type="button"
      className="flex aspect-square flex-1 flex-col items-center justify-center rounded-2xl border border-dashed border-white/15 bg-white/[0.03] text-center transition hover:border-cyan-400/40 hover:bg-cyan-500/[0.04]"
    >
      <span className="mb-2 text-xl">⤴</span>
      <span className="text-sm font-medium text-[#f0f6fc]">画像を追加</span>
      <span className="mt-1 text-xs text-[#8b949e]">JPG / PNG</span>
    </button>
  )
}

export function LeftPanel() {
  return (
    <aside className="flex h-full flex-col rounded-[28px] border border-white/10 bg-[#161b22] p-5 shadow-[0_24px_80px_rgba(0,0,0,0.35)]">
      <div className="mb-6 flex items-center gap-2">
        <span className="text-lg text-cyan-300">✦</span>
        <h2 className="text-lg font-semibold text-[#f0f6fc]">作りたいもの</h2>
      </div>

      <div className="space-y-6">
        <div>
          <SectionTitle>プロンプト</SectionTitle>
          <div className="rounded-3xl border border-white/10 bg-[#0d1117] p-4">
            <textarea
              readOnly
              value={promptText}
              className="h-44 w-full resize-none bg-transparent text-sm leading-7 text-[#f0f6fc] outline-none placeholder:text-[#8b949e]"
            />
            <div className="mt-3 flex items-center justify-between text-xs text-[#8b949e]">
              <span className="text-cyan-300">✦</span>
              <span>99 / 1000</span>
            </div>
          </div>
        </div>

        <div>
          <SectionTitle info>参考画像 (任意)</SectionTitle>
          <div className="grid grid-cols-2 gap-3">
            <UploadedThumb />
            <EmptyUploadSlot />
          </div>
        </div>

        <div className="space-y-3">
          <button
            type="button"
            className="flex w-full items-center justify-between rounded-2xl bg-gradient-to-r from-cyan-500 to-sky-500 px-4 py-3.5 text-sm font-semibold text-white shadow-[0_16px_40px_rgba(6,182,212,0.28)] transition hover:brightness-110"
          >
            <span className="flex items-center gap-2">
              <span>🖼</span>
              <span>画像を生成</span>
            </span>
            <span>›</span>
          </button>
          <button
            type="button"
            className="flex w-full items-center justify-between rounded-2xl bg-gradient-to-r from-purple-500 to-fuchsia-500 px-4 py-3.5 text-sm font-semibold text-white shadow-[0_16px_40px_rgba(168,85,247,0.28)] transition hover:brightness-110"
          >
            <span className="flex items-center gap-2">
              <span>⬡</span>
              <span>3D化する</span>
            </span>
            <span>✦</span>
          </button>
        </div>

        <div>
          <SectionTitle>クイック提案</SectionTitle>
          <div className="flex flex-wrap gap-2.5">
            {quickIdeas.map((idea) => (
              <button
                key={idea}
                type="button"
                className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-2 text-sm text-[#c9d1d9] transition hover:border-cyan-400/35 hover:text-white"
              >
                ○ {idea}
              </button>
            ))}
          </div>
        </div>
      </div>

      <div className="mt-auto rounded-2xl border border-yellow-300/10 bg-yellow-400/[0.05] px-4 py-3 text-sm text-[#c9d1d9]">
        <div className="flex items-start gap-3">
          <span className="mt-0.5 text-yellow-300">💡</span>
          <p>ヒント: 具体的に書くほど、理想のデザインに近づきます！</p>
        </div>
      </div>
    </aside>
  )
}
