import React, { useState } from 'react';
import { Play, Trash2, Database, Zap, Activity, Clock, Layers } from 'lucide-react';
import { toast } from 'react-hot-toast';

export default function BenchmarkPage() {
  const [loading, setLoading] = useState(false);
  const [status, setStatus] = useState<'idle' | 'seeding' | 'running' | 'cleaning'>('idle');
  const [results, setResults] = useState<any>(null);

  const handleSeed = async () => {
    setLoading(true);
    setStatus('seeding');
    try {
      const response = await fetch('http://localhost:5100/api/benchmark/seed?targetNodes=10000', { method: 'POST' });
      const data = await response.json();
      if (response.ok) {
        toast.success(`Seeded ${data.totalNodes ?? 0} nodes in ${(data.timeTakenSeconds ?? 0).toFixed(2)}s`);
      } else {
        toast.error(data.message || 'Failed to seed data');
      }
    } catch (error) {
      toast.error('Failed to seed benchmark data');
    } finally {
      setLoading(false);
      setStatus('idle');
    }
  };

  const handleRun = async () => {
    setLoading(true);
    setStatus('running');
    try {
      const response = await fetch('http://localhost:5100/api/benchmark/run', { method: 'POST' });
      const data = await response.json();
      if (response.ok) {
        setResults(data);
        toast.success(`MRP Benchmark completed in ${(data.timeTakenSeconds ?? 0).toFixed(2)}s`);
      } else {
        toast.error(data.message || 'Failed to run benchmark');
      }
    } catch (error) {
      toast.error('Failed to run benchmark');
    } finally {
      setLoading(false);
      setStatus('idle');
    }
  };

  const handleCleanup = async () => {
    setLoading(true);
    setStatus('cleaning');
    try {
      await fetch('http://localhost:5100/api/benchmark/cleanup', { method: 'DELETE' });
      toast.success('Benchmark data cleaned up');
      setResults(null);
    } catch (error) {
      toast.error('Failed to cleanup data');
    } finally {
      setLoading(false);
      setStatus('idle');
    }
  };

  return (
    <div className="p-6 space-y-6 page-enter">
      <div className="flex justify-between items-center mb-8">
        <div>
          <h1 className="text-3xl font-bold tracking-tight mb-2">MRP Performance Benchmark</h1>
          <p className="text-slate-400">Stress testing the MRP engine with 10,000+ BOM nodes.</p>
        </div>
        <div className="flex gap-2">
          <button className="btn btn-ghost" onClick={handleCleanup} disabled={loading}>
            <Trash2 className="w-4 h-4 mr-2" />
            Cleanup
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8" style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '24px' }}>
        <div className="glass-card p-6 border-lux" style={{ borderLeft: '4px solid var(--lux-gold)' }}>
          <div className="flex items-center gap-2 mb-4">
            <Database className="w-5 h-5 text-lux-gold" />
            <h3 className="text-lg font-bold">1. Seed Data</h3>
          </div>
          <p className="text-sm text-slate-400 mb-6">Generate a complex 10,000+ node BOM structure.</p>
          <button className="btn btn-primary w-full" onClick={handleSeed} disabled={loading}>
            {status === 'seeding' ? 'Seeding...' : 'Generate 10k Nodes'}
          </button>
        </div>

        <div className="glass-card p-6" style={{ borderLeft: '4px solid var(--amber-500)' }}>
          <div className="flex items-center gap-2 mb-4">
            <Play className="w-5 h-5 text-amber-500" />
            <h3 className="text-lg font-bold">2. Run MRP</h3>
          </div>
          <p className="text-sm text-slate-400 mb-6">Calculate requirements for the entire tree.</p>
          <button className="btn btn-primary w-full" style={{ background: 'linear-gradient(135deg, #b36a00, #d47f00)', color: 'white' }} onClick={handleRun} disabled={loading}>
            {status === 'running' ? 'Processing...' : 'Run Benchmark'}
          </button>
        </div>

        <div className="glass-card p-6" style={{ borderLeft: '4px solid var(--emerald-500)' }}>
          <div className="flex items-center gap-2 mb-4">
            <Zap className="w-5 h-5 text-emerald-500" />
            <h3 className="text-lg font-bold">3. Results</h3>
          </div>
          <p className="text-sm text-slate-400 mb-4">Performance metrics and throughput.</p>
          {results ? (
            <div className="space-y-2 text-sm">
              <div className="flex justify-between py-1 border-b border-white/5">
                <span>Time:</span>
                <span className="font-mono text-lux-gold">{(results.timeTakenSeconds ?? 0).toFixed(3)}s</span>
              </div>
              <div className="flex justify-between py-1 border-b border-white/5">
                <span>Processed:</span>
                <span className="font-mono">{results.totalItemsProcessed ?? 0} items</span>
              </div>
              <div className="flex justify-between py-1">
                <span>Speed:</span>
                <span className="status-badge status-active">{(results.nodesPerSecond ?? 0).toFixed(0)} nodes/s</span>
              </div>
            </div>
          ) : (
            <div className="text-center text-slate-500 py-4 italic">No results yet</div>
          )}
        </div>
      </div>

      {results && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4" style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '16px' }}>
          <div className="glass-card p-4">
            <div className="flex items-center gap-4">
              <div className="p-2 bg-blue-500/10 rounded-lg">
                <Activity className="w-6 h-6 text-blue-500" />
              </div>
              <div>
                <p className="text-xs text-slate-500 uppercase font-bold">Run Number</p>
                <p className="text-xl font-bold font-mono">{results.runNumber}</p>
              </div>
            </div>
          </div>
          <div className="glass-card p-4">
            <div className="flex items-center gap-4">
              <div className="p-2 bg-purple-500/10 rounded-lg">
                <Layers className="w-6 h-6 text-purple-500" />
              </div>
              <div>
                <p className="text-xs text-slate-500 uppercase font-bold">Planned Orders</p>
                <p className="text-xl font-bold font-mono text-emerald-400">{results.plannedOrdersGenerated}</p>
              </div>
            </div>
          </div>
          <div className="glass-card p-4">
            <div className="flex items-center gap-4">
              <div className="p-2 bg-orange-500/10 rounded-lg">
                <Clock className="w-6 h-6 text-orange-500" />
              </div>
              <div>
                <p className="text-xs text-slate-500 uppercase font-bold">Latency</p>
                <p className="text-xl font-bold font-mono text-amber-400">
                  {results.totalItemsProcessed > 0 
                    ? (results.timeTakenMilliseconds / results.totalItemsProcessed).toFixed(2) 
                    : '0.00'} ms/n
                </p>
              </div>
            </div>
          </div>
          <div className="glass-card p-4">
            <div className="flex items-center gap-4">
              <div className="p-2 bg-green-500/10 rounded-lg">
                <Zap className="w-6 h-6 text-green-500" />
              </div>
              <div>
                <p className="text-xs text-slate-500 uppercase font-bold">Throughput</p>
                <p className="text-xl font-bold font-mono text-lux-gold">{(results.nodesPerSecond ?? 0).toFixed(0)} n/s</p>
              </div>
            </div>
          </div>
        </div>
      )}

      {loading && (
        <div className="fixed inset-0 bg-navy-950/80 backdrop-blur-sm flex items-center justify-center z-50">
          <div className="max-w-md w-full p-8 glass-card text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-lux-gold mx-auto mb-6"></div>
            <h3 className="text-xl font-semibold mb-2">
              {status === 'seeding' ? 'Generating 10,000+ BOM Nodes...' : 
               status === 'running' ? 'Executing Recursive MRP Calculation...' : 
               'Cleaning up benchmark data...'}
            </h3>
            <p className="text-slate-400 mb-6">This may take a minute due to database complexity.</p>
            <div className="progress-bar">
               <div className="progress-fill" style={{ width: status === 'seeding' ? '45%' : '75%', background: 'var(--lux-gold)' }} />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
