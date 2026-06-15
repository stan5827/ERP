import React, { useState, useEffect } from 'react';
import { 
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, 
  PieChart, Pie, Cell, LineChart, Line 
} from 'recharts';
import { 
  TrendingUp, TrendingDown, DollarSign, PieChart as PieIcon, 
  BarChart3, Activity, Calendar, Filter, Download
} from 'lucide-react';
import axios from 'axios';
import { format } from 'date-fns';

const COLORS = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#6366F1'];

const CostAnalysisDashboard: React.FC = () => {
  const [categoryData, setCategoryData] = useState<any[]>([]);
  const [departmentData, setDepartmentData] = useState<any[]>([]);
  const [trendData, setTrendData] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [catRes, depRes, trendRes] = await Promise.all([
          axios.get('/api/cost-analysis/categories'),
          axios.get('/api/cost-analysis/departments'),
          axios.get('/api/cost-analysis/trends')
        ]);
        
        setCategoryData(catRes.data.data);
        setDepartmentData(depRes.data.data);
        setTrendData(trendRes.data.data);
      } catch (error) {
        console.error('Failed to fetch cost analysis data', error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  if (loading) {
    return <div className="p-8 text-center">正在載入成本分析數據...</div>;
  }

  const totalActual = categoryData.reduce((acc, curr) => acc + curr.totalActualCost, 0);
  const totalStandard = categoryData.reduce((acc, curr) => acc + curr.totalStandardCost, 0);
  const totalVariance = totalActual - totalStandard;

  return (
    <div className="p-6 bg-slate-50 min-h-screen space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold text-slate-800 flex items-center gap-2">
          <Activity className="text-blue-600" />
          多維度成本分析中心
        </h1>
        <div className="flex gap-2">
          <button className="flex items-center gap-1 bg-white border border-slate-200 px-3 py-1.5 rounded-md text-sm hover:bg-slate-50">
            <Calendar size={16} /> 最近 30 天
          </button>
          <button className="flex items-center gap-1 bg-white border border-slate-200 px-3 py-1.5 rounded-md text-sm hover:bg-slate-50">
            <Filter size={16} /> 篩選
          </button>
          <button className="flex items-center gap-1 bg-blue-600 text-white px-3 py-1.5 rounded-md text-sm hover:bg-blue-700">
            <Download size={16} /> 導出報表
          </button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 bg-blue-100 text-blue-600 rounded-lg">
              <DollarSign size={20} />
            </div>
            <span className="text-sm font-medium text-slate-500">總實際成本</span>
          </div>
          <div className="text-2xl font-bold text-slate-900">${totalActual.toLocaleString()}</div>
        </div>
        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 bg-emerald-100 text-emerald-600 rounded-lg">
              <PieIcon size={20} />
            </div>
            <span className="text-sm font-medium text-slate-500">總標準成本</span>
          </div>
          <div className="text-2xl font-bold text-slate-900">${totalStandard.toLocaleString()}</div>
        </div>
        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 bg-amber-100 text-amber-600 rounded-lg">
              <BarChart3 size={20} />
            </div>
            <span className="text-sm font-medium text-slate-500">總差異金額</span>
          </div>
          <div className={`text-2xl font-bold ${totalVariance > 0 ? 'text-red-600' : 'text-emerald-600'}`}>
            {totalVariance > 0 ? '+' : ''}${totalVariance.toLocaleString()}
          </div>
        </div>
        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 bg-indigo-100 text-indigo-600 rounded-lg">
              <TrendingUp size={20} />
            </div>
            <span className="text-sm font-medium text-slate-500">平均差異率</span>
          </div>
          <div className={`text-2xl font-bold ${totalVariance > 0 ? 'text-red-600' : 'text-emerald-600'}`}>
            {totalStandard > 0 ? Math.round((totalVariance / totalStandard) * 100) : 0}%
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Trend Chart */}
        <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
          <h3 className="text-lg font-semibold text-slate-800 mb-4 flex items-center gap-2">
            <Activity size={18} className="text-blue-500" />
            成本趨勢分析
          </h3>
          <div className="h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={trendData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="period" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Line type="monotone" dataKey="actualCost" name="實際成本" stroke="#3B82F6" strokeWidth={2} dot={{ r: 4 }} />
                <Line type="monotone" dataKey="standardCost" name="標準成本" stroke="#10B981" strokeWidth={2} strokeDasharray="5 5" />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Category Breakdown */}
        <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
          <h3 className="text-lg font-semibold text-slate-800 mb-4 flex items-center gap-2">
            <PieIcon size={18} className="text-emerald-500" />
            產品類別成本分佈
          </h3>
          <div className="h-[300px]">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={categoryData}
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={100}
                  paddingAngle={5}
                  dataKey="totalActualCost"
                  nameKey="categoryName"
                  label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                >
                  {categoryData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Department Variance */}
        <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm col-span-full">
          <h3 className="text-lg font-semibold text-slate-800 mb-4 flex items-center gap-2">
            <BarChart3 size={18} className="text-amber-500" />
            部門差異細分 (料、工、費變異)
          </h3>
          <div className="h-[400px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={departmentData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="departmentName" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Bar dataKey="materialVariance" name="材料差異" fill="#3B82F6" radius={[4, 4, 0, 0]} />
                <Bar dataKey="laborVariance" name="人工差異" fill="#10B981" radius={[4, 4, 0, 0]} />
                <Bar dataKey="machineVariance" name="製造費用差異" fill="#F59E0B" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CostAnalysisDashboard;
