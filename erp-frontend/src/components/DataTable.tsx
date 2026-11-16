import React, { useState, useMemo } from 'react';
import './DataTable.css';

export type Column<T> = {
  key: string;
  title: string;
  width?: string | number;
  sortable?: boolean;
  filterable?: boolean;
  render?: (value: unknown, row: T, index: number) => React.ReactNode;
  align?: 'left' | 'center' | 'right';
};

type DataTableProps<T> = {
  columns: Column<T>[];
  data: T[];
  keyField: keyof T;
  pageSize?: number;
  showPagination?: boolean;
  showRowNumbers?: boolean;
  loading?: boolean;
  emptyMessage?: string;
  onRowClick?: (row: T) => void;
  onPageChange?: (page: number, pageSize: number) => void;
  onSort?: (key: string, direction: 'asc' | 'desc') => void;
  onFilter?: (filters: Record<string, string>) => void;
}

export function DataTable<T>({
  columns,
  data,
  keyField,
  pageSize = 10,
  showPagination = true,
  showRowNumbers = false,
  loading = false,
  emptyMessage = '暂无数据',
  onRowClick,
  onPageChange,
  onSort,
}: DataTableProps<T>) {
  const [currentPage, setCurrentPage] = useState(1);
  const [sortKey, setSortKey] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');

  const handleSort = (key: string) => {
    if (sortKey === key) {
      const newDirection = sortDirection === 'asc' ? 'desc' : 'asc';
      setSortDirection(newDirection);
      onSort?.(key, newDirection);
    } else {
      setSortKey(key);
      setSortDirection('asc');
      onSort?.(key, 'asc');
    }
  };

  const sortedData = useMemo(() => {
    if (!sortKey) return data;
    
    return [...data].sort((a, b) => {
      const aVal = a[sortKey as keyof T];
      const bVal = b[sortKey as keyof T];
      
      if (aVal === bVal) return 0;
      if (aVal === null || aVal === undefined) return 1;
      if (bVal === null || bVal === undefined) return -1;
      
      const comparison = aVal < bVal ? -1 : 1;
      return sortDirection === 'asc' ? comparison : -comparison;
    });
  }, [data, sortKey, sortDirection]);

  const totalPages = Math.ceil(sortedData.length / pageSize);
  const startIndex = (currentPage - 1) * pageSize;
  const paginatedData = showPagination 
    ? sortedData.slice(startIndex, startIndex + pageSize)
    : sortedData;

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
    onPageChange?.(page, pageSize);
  };

  const renderPagination = () => {
    if (!showPagination || totalPages <= 1) return null;

    const pages: (number | string)[] = [];
    const maxVisible = 5;
    
    if (totalPages <= maxVisible) {
      for (let i = 1; i <= totalPages; i++) pages.push(i);
    } else {
      pages.push(1);
      if (currentPage > 3) pages.push('...');
      
      const start = Math.max(2, currentPage - 1);
      const end = Math.min(totalPages - 1, currentPage + 1);
      
      for (let i = start; i <= end; i++) pages.push(i);
      
      if (currentPage < totalPages - 2) pages.push('...');
      pages.push(totalPages);
    }

    return (
      <div className="table-pagination">
        <button
          className="pagination-btn"
          onClick={() => handlePageChange(1)}
          disabled={currentPage === 1}
          title="第一页"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polyline points="11 17 6 12 11 7" />
            <polyline points="18 17 13 12 18 7" />
          </svg>
        </button>
        <button
          className="pagination-btn"
          onClick={() => handlePageChange(currentPage - 1)}
          disabled={currentPage === 1}
          title="上一页"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polyline points="15 18 9 12 15 6" />
          </svg>
        </button>
        
        {pages.map((page, i) => (
          typeof page === 'number' ? (
            <button
              key={i}
              className={`pagination-btn pagination-page ${currentPage === page ? 'active' : ''}`}
              onClick={() => handlePageChange(page)}
            >
              {page}
            </button>
          ) : (
            <span key={i} className="pagination-ellipsis">{page}</span>
          )
        ))}
        
        <button
          className="pagination-btn"
          onClick={() => handlePageChange(currentPage + 1)}
          disabled={currentPage === totalPages}
          title="下一页"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polyline points="9 18 15 12 9 6" />
          </svg>
        </button>
        <button
          className="pagination-btn"
          onClick={() => handlePageChange(totalPages)}
          disabled={currentPage === totalPages}
          title="最后一页"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polyline points="13 17 18 12 13 7" />
            <polyline points="6 17 11 12 6 7" />
          </svg>
        </button>
      </div>
    );
  };

  const renderHeader = () => (
    <thead>
      <tr>
        {showRowNumbers && <th className="th-row-number">#</th>}
        {columns.map(col => (
          <th
            key={col.key}
            style={{ 
              width: col.width,
              textAlign: col.align || 'left'
            }}
            className={col.sortable ? 'sortable' : ''}
            onClick={() => col.sortable && handleSort(col.key)}
          >
            <div className="th-content">
              <span>{col.title}</span>
              {col.sortable && (
                <span className={`sort-icon ${sortKey === col.key ? 'active' : ''}`}>
                  {sortKey === col.key ? (
                    sortDirection === 'asc' ? (
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <polyline points="18 15 12 9 6 15" />
                      </svg>
                    ) : (
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <polyline points="6 9 12 15 18 9" />
                      </svg>
                    )
                  ) : (
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <polyline points="6 9 12 15 18 9" />
                    </svg>
                  )}
                </span>
              )}
            </div>
          </th>
        ))}
      </tr>
    </thead>
  );

  const renderBody = () => {
    if (loading) {
      return (
        <tbody>
          <tr>
            <td colSpan={columns.length + (showRowNumbers ? 1 : 0)} className="loading-cell">
              <div className="table-loading">
                <div className="loading-spinner medium">
                  <div className="spinner-ring"></div>
                  <div className="spinner-ring"></div>
                  <div className="spinner-ring"></div>
                </div>
                <span>加载中...</span>
              </div>
            </td>
          </tr>
        </tbody>
      );
    }

    if (paginatedData.length === 0) {
      return (
        <tbody>
          <tr>
            <td colSpan={columns.length + (showRowNumbers ? 1 : 0)} className="empty-cell">
              {emptyMessage}
            </td>
          </tr>
        </tbody>
      );
    }

    return (
      <tbody>
        {paginatedData.map((row, index) => (
          <tr 
            key={String(row[keyField])} 
            onClick={() => onRowClick?.(row)}
            className={onRowClick ? 'clickable' : ''}
          >
            {showRowNumbers && (
              <td className="td-row-number">{startIndex + index + 1}</td>
            )}
            {columns.map(col => (
              <td 
                key={col.key} 
                style={{ textAlign: col.align || 'left' }}
              >
                {col.render 
                  ? col.render(row[col.key as keyof T], row, index)
                  : String(row[col.key as keyof T] ?? '')
                }
              </td>
            ))}
          </tr>
        ))}
      </tbody>
    );
  };

  return (
    <div className="data-table-wrapper">
      <div className="data-table-container">
        <table className="data-table">
          {renderHeader()}
          {renderBody()}
        </table>
      </div>
      {renderPagination()}
    </div>
  );
}