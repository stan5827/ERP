import React, { useEffect, useState } from "react";
import { Search, Filter, Clock, FileText, ChevronRight, DollarSign, Calendar, Info } from "lucide-react";
import toast from "react-hot-toast";

interface RfqItem {
  id: string;
  skuId: string;
  skuNumber: string;
  skuName: string;
  quantity: number;
  unit: string;
  specifications: string;
}

interface Rfq {
  id: string;
  rfqNumber: string;
  rfqDate: string;
  deadline: string;
  status: string;
  itemCount: number;
  items: RfqItem[];
}

const SupplierRfqList: React.FC = () => {
  const [rfqs, setRfqs] = useState<Rfq[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");

  useEffect(() => {
    fetchRfqs();
  }, []);

  const fetchRfqs = async () => {
    setIsLoading(true);
    const supplierId = localStorage.getItem("supplier_id");
    try {
      const response = await fetch(`http://localhost:5100/api/supplier-portal/rfqs?supplierId=${supplierId}`, {
        headers: {
          "Authorization": `Bearer ${localStorage.getItem("supplier_token")}`
        }
      });
      const result = await response.json();
      if (result.success) {
        setRfqs(result.data);
      } else {
        toast.error("獲取詢價單失敗");
      }
    } catch (error) {
      console.error("Fetch RFQs error:", error);
      // Mock data for demo
      setRfqs([
        {
          id: "1",
          rfqNumber: "RFQ-2026-0001",
          rfqDate: "2026-04-20",
          deadline: "2026-05-10",
          status: "open",
          itemCount: 2,
          items: [
            { id: "i1", skuId: "s1", skuNumber: "RM-001", skuName: "鋁合金外殼", quantity: 500, unit: "PCS", specifications: "表面陽極處理, 黑色" },
            { id: "i2", skuId: "s2", skuNumber: "RM-002", skuName: "散熱鰭片", quantity: 1000, unit: "PCS", specifications: "AL6061-T6" }
          ]
        }
      ]);
    } finally {
      setIsLoading(false);
    }
  };

  const filteredRfqs = rfqs.filter(r => 
    r.rfqNumber.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="space-y-8 animate-in fade-in duration-700">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-white tracking-tight">待處理詢價單</h1>
          <p className="text-gray-400 mt-1">Pending Requests for Quotation (RFQs)</p>
        </div>
        
        <div className="flex items-center gap-3">
          <div className="relative group">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 group-focus-within:text-blue-500 transition-colors" size={18} />
            <input 
              type="text" 
              placeholder="搜尋詢價單號..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="bg-[#0d1829] border border-[#1a3050] rounded-xl py-2 pl-10 pr-4 text-sm focus:border-blue-500 outline-none w-64 transition-all"
            />
          </div>
          <button className="p-2 bg-[#0d1829] border border-[#1a3050] rounded-xl hover:bg-[#1a3050] transition-colors">
            <Filter size={18} className="text-gray-400" />
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[1, 2, 3].map(i => (
            <div key={i} className="h-64 bg-[#0d1829]/50 border border-[#1a3050] rounded-3xl animate-pulse"></div>
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredRfqs.map((rfq) => (
            <div 
              key={rfq.id}
              className="bg-[#0d1829]/60 backdrop-blur-md border border-[#1a3050] rounded-3xl p-6 hover:border-blue-500/50 transition-all group hover:shadow-2xl hover:shadow-blue-500/5 relative overflow-hidden"
            >
              <div className="absolute top-0 right-0 p-4 opacity-10 group-hover:opacity-20 transition-opacity">
                <FileText size={80} className="text-blue-500" />
              </div>

              <div className="flex items-start justify-between mb-6 relative z-10">
                <div>
                  <div className="flex items-center gap-2 mb-1">
                    <span className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></span>
                    <span className="text-xs font-bold text-green-500 uppercase tracking-widest">開放中</span>
                  </div>
                  <h3 className="text-xl font-bold text-white group-hover:text-blue-400 transition-colors">{rfq.rfqNumber}</h3>
                </div>
                <div className="bg-blue-500/10 p-2 rounded-xl border border-blue-500/20 text-blue-400">
                  <Clock size={20} />
                </div>
              </div>

              <div className="space-y-4 mb-8 relative z-10">
                <div className="flex items-center gap-3 text-sm">
                  <Calendar size={16} className="text-gray-500" />
                  <span className="text-gray-400">截止日期：</span>
                  <span className="text-white font-medium">{rfq.deadline || "不限"}</span>
                </div>
                <div className="flex items-center gap-3 text-sm">
                  <Info size={16} className="text-gray-500" />
                  <span className="text-gray-400">品項數量：</span>
                  <span className="text-white font-medium">{rfq.itemCount} 項物料</span>
                </div>
              </div>

              <div className="space-y-2 mb-8 relative z-10">
                {rfq.items.slice(0, 2).map((item, idx) => (
                  <div key={idx} className="flex items-center justify-between text-xs bg-[#1a3050]/30 p-2 rounded-lg border border-white/5">
                    <span className="text-gray-300 truncate max-w-[120px]">{item.skuName}</span>
                    <span className="text-blue-400 font-bold">{item.quantity} {item.unit}</span>
                  </div>
                ))}
                {rfq.itemCount > 2 && (
                  <p className="text-[10px] text-gray-500 text-center italic mt-1">...及其他 {rfq.itemCount - 2} 個品項</p>
                )}
              </div>

              <button 
                className="w-full bg-[#1a3050] hover:bg-blue-600 text-white font-bold py-3 rounded-2xl transition-all flex items-center justify-center gap-2 group relative z-10"
              >
                <DollarSign size={18} />
                立即報價
                <ChevronRight size={18} className="group-hover:translate-x-1 transition-transform" />
              </button>
            </div>
          ))}

          {filteredRfqs.length === 0 && (
            <div className="col-span-full py-20 text-center bg-[#0d1829]/30 rounded-3xl border border-dashed border-[#1a3050]">
              <FileText size={48} className="text-gray-600 mx-auto mb-4" />
              <p className="text-gray-500 font-medium">目前沒有符合條件的詢價單</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default SupplierRfqList;
