const fs = require('fs');
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, AlignmentType, HeadingLevel, BorderStyle, WidthType,
  ShadingType, PageBreak, PageNumber, LevelFormat, TableOfContents,
  TabStopType, TabStopPosition
} = require('docx');

// ── Shared styles ──
const border = { style: BorderStyle.SINGLE, size: 1, color: "CCCCCC" };
const borders = { top: border, bottom: border, left: border, right: border };
const cellMargins = { top: 80, bottom: 80, left: 120, right: 120 };
const headerShading = { fill: "1E3A5F", type: ShadingType.CLEAR };
const altShading = { fill: "F5F8FA", type: ShadingType.CLEAR };

function headerCell(text, width) {
  return new TableCell({
    borders, width: { size: width, type: WidthType.DXA }, shading: headerShading, margins: cellMargins,
    verticalAlign: "center",
    children: [new Paragraph({ alignment: AlignmentType.CENTER, children: [new TextRun({ text, bold: true, color: "FFFFFF", font: "Arial", size: 20 })] })]
  });
}
function cell(text, width, shading) {
  const opts = { borders, width: { size: width, type: WidthType.DXA }, margins: cellMargins,
    children: [new Paragraph({ children: [new TextRun({ text, font: "Arial", size: 20 })] })] };
  if (shading) opts.shading = shading;
  return new TableCell(opts);
}
function heading(text, level) {
  return new Paragraph({ heading: level, spacing: { before: 300, after: 150 }, children: [new TextRun({ text, font: "Arial" })] });
}
function para(text, opts = {}) {
  return new Paragraph({ spacing: { after: 120 }, ...opts, children: [new TextRun({ text, font: "Arial", size: 22, ...opts.run })] });
}
function bullet(text, ref = "bullets", level = 0) {
  return new Paragraph({ numbering: { reference: ref, level }, spacing: { after: 80 },
    children: [new TextRun({ text, font: "Arial", size: 22 })] });
}

// ── Content ──
const doc = new Document({
  styles: {
    default: { document: { run: { font: "Arial", size: 22 } } },
    paragraphStyles: [
      { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 36, bold: true, font: "Arial", color: "1E3A5F" },
        paragraph: { spacing: { before: 360, after: 200 }, outlineLevel: 0 } },
      { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 28, bold: true, font: "Arial", color: "2D6EF5" },
        paragraph: { spacing: { before: 240, after: 160 }, outlineLevel: 1 } },
      { id: "Heading3", name: "Heading 3", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 24, bold: true, font: "Arial", color: "333333" },
        paragraph: { spacing: { before: 200, after: 120 }, outlineLevel: 2 } },
    ]
  },
  numbering: {
    config: [
      { reference: "bullets", levels: [{ level: 0, format: LevelFormat.BULLET, text: "\u2022", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
      { reference: "numbers", levels: [{ level: 0, format: LevelFormat.DECIMAL, text: "%1.", alignment: AlignmentType.LEFT, style: { paragraph: { indent: { left: 720, hanging: 360 } } } }] },
    ]
  },
  sections: [
    // ── Cover Page ──
    {
      properties: {
        page: { size: { width: 12240, height: 15840 }, margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 } }
      },
      children: [
        new Paragraph({ spacing: { before: 3000 } }),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 200 }, children: [
          new TextRun({ text: "ERP AI Agent", font: "Arial", size: 56, bold: true, color: "1E3A5F" })
        ]}),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 400 }, children: [
          new TextRun({ text: "\u76E3\u63A7\u7CFB\u7D71\u898F\u683C\u66F8", font: "Arial", size: 44, color: "2D6EF5" })
        ]}),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 100 }, children: [
          new TextRun({ text: "\u88FD\u9020\u696D\u8CC7\u6E90\u898F\u5283\u7CFB\u7D71 \u00B7 AI \u667A\u80FD\u4EE3\u7406\u76E3\u63A7\u8207\u81EA\u52D5\u5316\u6D41\u7A0B", font: "Arial", size: 24, color: "666666" })
        ]}),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 400 }, children: [
          new TextRun({ text: "ProMES ERP v1.0", font: "Arial", size: 22, color: "999999" })
        ]}),
        new Paragraph({ alignment: AlignmentType.CENTER, spacing: { after: 100 }, children: [
          new TextRun({ text: "\u7248\u672C: 1.0 | \u65E5\u671F: 2026\u5E743\u670825\u65E5", font: "Arial", size: 20, color: "999999" })
        ]}),
        new Paragraph({ alignment: AlignmentType.CENTER, children: [
          new TextRun({ text: "\u5206\u985E: \u5167\u90E8\u6A5F\u5BC6", font: "Arial", size: 20, color: "CC0000", bold: true })
        ]}),
      ]
    },
    // ── TOC ──
    {
      properties: {
        page: { size: { width: 12240, height: 15840 }, margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 } }
      },
      headers: {
        default: new Header({ children: [new Paragraph({ children: [
          new TextRun({ text: "ERP AI Agent \u76E3\u63A7\u7CFB\u7D71\u898F\u683C\u66F8", font: "Arial", size: 18, color: "999999" }),
          new TextRun({ text: "\tProMES ERP v1.0", font: "Arial", size: 18, color: "999999" }),
        ], tabStops: [{ type: TabStopType.RIGHT, position: TabStopPosition.MAX }] })] })
      },
      footers: {
        default: new Footer({ children: [new Paragraph({
          alignment: AlignmentType.CENTER,
          children: [new TextRun({ text: "\u7B2C ", font: "Arial", size: 18 }), new TextRun({ children: [PageNumber.CURRENT], font: "Arial", size: 18 }), new TextRun({ text: " \u9801", font: "Arial", size: 18 })]
        })] })
      },
      children: [
        heading("\u76EE\u9304", HeadingLevel.HEADING_1),
        new TableOfContents("Table of Contents", { hyperlink: true, headingStyleRange: "1-3" }),
        new Paragraph({ children: [new PageBreak()] }),

        // ── 1. System Overview ──
        heading("1. \u7CFB\u7D71\u6982\u8FF0\u8207\u67B6\u69CB\u5716", HeadingLevel.HEADING_1),
        para("\u672C\u6587\u4EF6\u63CF\u8FF0 ProMES ERP \u7CFB\u7D71\u4E2D AI Agent \u76E3\u63A7\u5E73\u53F0\u7684\u67B6\u69CB\u3001\u529F\u80FD\u3001\u90E8\u7F72\u8207\u7DAD\u904B\u898F\u7BC4\u3002\u7CFB\u7D71\u6574\u5408 Prometheus + Grafana \u76E3\u63A7\u5806\u758A\uFF0C\u63D0\u4F9B\u5373\u6642\u7684 AI Agent \u6548\u80FD\u8FFD\u8E64\u3001\u6D41\u7A0B\u57F7\u884C\u76E3\u63A7\u8207\u7570\u5E38\u8B66\u5831\u3002"),
        heading("1.1 \u7CFB\u7D71\u67B6\u69CB", HeadingLevel.HEADING_2),
        bullet("\u524D\u7AEF: React 19 + TypeScript + Vite + Recharts"),
        bullet("\u5F8C\u7AEF: ASP.NET Core 8 + EF Core + SQL Server"),
        bullet("\u76E3\u63A7: Prometheus \u6307\u6A19\u66B8\u96C6 + Grafana \u5100\u8868\u677F"),
        bullet("\u6307\u6A19\u66B8\u96C6: prometheus-net.AspNetCore \u5957\u4EF6\uFF0C\u900F\u904E /metrics \u7AEF\u9EDE\u66B4\u9732"),
        heading("1.2 \u76E3\u63A7\u6307\u6A19\u5B9A\u7FA9", HeadingLevel.HEADING_2),
        new Table({
          width: { size: 9360, type: WidthType.DXA },
          columnWidths: [2800, 1200, 5360],
          rows: [
            new TableRow({ children: [headerCell("\u6307\u6A19\u540D\u7A31", 2800), headerCell("\u985E\u578B", 1200), headerCell("\u8AAA\u660E", 5360)] }),
            new TableRow({ children: [cell("erp_api_requests_total", 2800), cell("Counter", 1200), cell("API \u8ACB\u6C42\u7E3D\u6578 (\u542B endpoint/method/status_code)", 5360, altShading)] }),
            new TableRow({ children: [cell("erp_api_request_duration_seconds", 2800), cell("Histogram", 1200), cell("API \u56DE\u61C9\u5EF6\u9072 (P50/P95/P99)", 5360)] }),
            new TableRow({ children: [cell("erp_ai_agent_latency_seconds", 2800), cell("Histogram", 1200), cell("AI Agent \u8655\u7406\u5EF6\u9072", 5360, altShading)] }),
            new TableRow({ children: [cell("erp_ai_agent_errors_total", 2800), cell("Counter", 1200), cell("AI Agent \u932F\u8AA4\u6578", 5360)] }),
            new TableRow({ children: [cell("erp_ai_agent_tokens_total", 2800), cell("Counter", 1200), cell("AI Agent token \u6D88\u8017\u91CF", 5360, altShading)] }),
            new TableRow({ children: [cell("erp_ai_agent_health_score", 2800), cell("Gauge", 1200), cell("Agent \u5065\u5EB7\u5206\u6578 (0-1)", 5360)] }),
            new TableRow({ children: [cell("erp_work_orders_active", 2800), cell("Gauge", 1200), cell("\u6D3B\u8E8D\u5DE5\u55AE\u6578", 5360, altShading)] }),
            new TableRow({ children: [cell("erp_inventory_low_stock_count", 2800), cell("Gauge", 1200), cell("\u4F4E\u5EAB\u5B58\u54C1\u9805\u6578", 5360)] }),
            new TableRow({ children: [cell("erp_agent_flow_executions_total", 2800), cell("Counter", 1200), cell("Agent Flow \u57F7\u884C\u6B21\u6578", 5360, altShading)] }),
            new TableRow({ children: [cell("erp_agent_flow_duration_seconds", 2800), cell("Histogram", 1200), cell("Agent Flow \u57F7\u884C\u8017\u6642", 5360)] }),
          ]
        }),

        new Paragraph({ children: [new PageBreak()] }),
        // ── 2. Agent Flow Design ──
        heading("2. Agent Flow \u6D41\u7A0B\u8A2D\u8A08", HeadingLevel.HEADING_1),
        para("\u7CFB\u7D71\u5BE6\u4F5C\u4E09\u500B\u88FD\u9020\u696D\u5178\u578B\u5834\u666F\u7684 Agent Flow\uFF0C\u6BCF\u500B\u6D41\u7A0B\u5305\u542B\u89F8\u767C\u3001\u57F7\u884C\u3001\u56DE\u5BEB\u4E09\u500B\u968E\u6BB5\u3002"),
        heading("2.1 \u7522\u7DDA\u7570\u5E38\u6D3E\u5DE5", HeadingLevel.HEADING_2),
        para("\u89F8\u767C\u689D\u4EF6: AI \u5075\u6E2C\u7522\u7DDA\u7570\u5E38 (IoT \u4FE1\u865F/AI Vision)"),
        bullet("\u6B65\u9A5F 1: AI \u5206\u6790\u7570\u5E38\u985E\u578B\u8207\u56B4\u91CD\u5EA6"),
        bullet("\u6B65\u9A5F 2: \u81EA\u52D5\u5EFA\u7ACB\u7DAD\u4FEE\u55AE \u2192 POST /api/work-orders"),
        bullet("\u6B65\u9A5F 3: \u67E5\u8A62\u53EF\u7528\u7DAD\u4FEE\u4EBA\u54E1 \u2192 GET /api/employees?dept=maintenance"),
        bullet("\u6B65\u9A5F 4: \u6D3E\u5DE5\u4E26\u56DE\u5BEB ERP \u2192 PATCH /api/work-orders/{id}/assign"),
        bullet("\u6B65\u9A5F 5: \u901A\u77E5 LINE \u63A8\u64AD \u2192 POST /api/digital/line-notify"),
        heading("2.2 \u5BA2\u670D\u8A62\u50F9\u81EA\u52D5\u5316", HeadingLevel.HEADING_2),
        para("\u89F8\u767C\u689D\u4EF6: LINE/\u5B98\u7DB2\u8A62\u50F9\u8A0A\u606F\u9032\u5165"),
        bullet("\u6B65\u9A5F 1: NLP \u89E3\u6790\u9700\u6C42 (\u7522\u54C1\u3001\u6578\u91CF\u3001\u4EA4\u671F)"),
        bullet("\u6B65\u9A5F 2: \u67E5\u5EAB\u5B58 \u2192 GET /api/inventory?sku={sku}"),
        bullet("\u6B65\u9A5F 3: \u6BD4\u5C0D\u4F9B\u61C9\u93C8\u4EA4\u671F \u2192 GET /api/suppliers/{id}/lead-time"),
        bullet("\u6B65\u9A5F 4: \u751F\u6210\u5831\u50F9\u65B9\u6848 \u2192 POST /api/quotes"),
        bullet("\u6B65\u9A5F 5: \u56DE\u50B3 LINE/\u5B98\u7DB2 \u2192 \u56DE\u8986\u5831\u50F9 PDF"),
        heading("2.3 \u5EAB\u5B58\u9810\u8B66\u88DC\u8CA8", HeadingLevel.HEADING_2),
        para("\u89F8\u767C\u689D\u4EF6: \u5B9A\u6642\u6392\u7A0B / \u5EAB\u5B58\u4F4E\u65BC\u5B89\u5168\u6C34\u4F4D"),
        bullet("\u6B65\u9A5F 1: \u6383\u63CF\u4F4E\u5EAB\u5B58 \u2192 GET /api/inventory/low-stock"),
        bullet("\u6B65\u9A5F 2: AI \u9810\u6E2C\u6700\u4F73\u88DC\u8CA8\u91CF (\u6B77\u53F2\u6D88\u8017 + \u9700\u6C42\u9810\u6E2C)"),
        bullet("\u6B65\u9A5F 3: \u6BD4\u5C0D\u4F9B\u61C9\u5546\u8207\u50F9\u683C \u2192 GET /api/suppliers?sku={sku}"),
        bullet("\u6B65\u9A5F 4: \u81EA\u52D5\u751F\u6210\u63A1\u8CFC\u7533\u8ACB \u2192 POST /api/purchase-requisitions"),
        bullet("\u6B65\u9A5F 5: \u9032\u5165\u5BE9\u6279\u6D41\u7A0B \u2192 POST /api/approval-workflow/submit"),

        new Paragraph({ children: [new PageBreak()] }),
        // ── 3. Grafana Configuration ──
        heading("3. Grafana \u914D\u7F6E\u6307\u5357", HeadingLevel.HEADING_1),
        heading("3.1 \u5FEB\u901F\u555F\u52D5", HeadingLevel.HEADING_2),
        para("\u4F7F\u7528 Docker Compose \u4E00\u9375\u555F\u52D5 Prometheus + Grafana:"),
        new Paragraph({ spacing: { after: 120 }, children: [
          new TextRun({ text: "cd monitoring && docker-compose up -d", font: "Courier New", size: 20, color: "2D6EF5" })
        ]}),
        bullet("Prometheus: http://localhost:9090"),
        bullet("Grafana: http://localhost:3000 (admin / admin123)"),
        heading("3.2 \u5100\u8868\u677F\u9762\u677F", HeadingLevel.HEADING_2),
        new Table({
          width: { size: 9360, type: WidthType.DXA },
          columnWidths: [2000, 2800, 4560],
          rows: [
            new TableRow({ children: [headerCell("\u9762\u677F", 2000), headerCell("PromQL \u67E5\u8A62", 2800), headerCell("\u7528\u9014", 4560)] }),
            new TableRow({ children: [cell("Agent \u5EF6\u9072", 2000), cell("histogram_quantile(0.95, rate(erp_ai_agent_latency_seconds_bucket[5m]))", 2800), cell("95% \u97FF\u61C9\u6642\u9593", 4560, altShading)] }),
            new TableRow({ children: [cell("\u932F\u8AA4\u7387", 2000), cell("rate(erp_ai_agent_errors_total[5m]) / rate(erp_api_requests_total[5m])", 2800), cell("<1% \u8B66\u5831", 4560)] }),
            new TableRow({ children: [cell("ERP QPS", 2000), cell("rate(erp_api_requests_total[5m])", 2800), cell("\u7522\u7DDA\u8CA0\u8F09", 4560, altShading)] }),
            new TableRow({ children: [cell("Token \u6210\u672C", 2000), cell("sum(increase(erp_ai_agent_tokens_total[1h]))", 2800), cell("ROI \u8FFD\u8E64", 4560)] }),
          ]
        }),

        new Paragraph({ children: [new PageBreak()] }),
        // ── 4. Deployment ──
        heading("4. \u90E8\u7F72\u8207\u7DAD\u904B\u624B\u518A", HeadingLevel.HEADING_1),
        heading("4.1 \u90E8\u7F72\u6B65\u9A5F", HeadingLevel.HEADING_2),
        new Paragraph({ numbering: { reference: "numbers", level: 0 }, spacing: { after: 80 },
          children: [new TextRun({ text: "\u5B89\u88DD prometheus-net.AspNetCore \u5957\u4EF6", font: "Arial", size: 22 })] }),
        new Paragraph({ numbering: { reference: "numbers", level: 0 }, spacing: { after: 80 },
          children: [new TextRun({ text: "\u555F\u7528 /metrics \u7AEF\u9EDE (\u53C3\u8003 Program.cs)", font: "Arial", size: 22 })] }),
        new Paragraph({ numbering: { reference: "numbers", level: 0 }, spacing: { after: 80 },
          children: [new TextRun({ text: "\u90E8\u7F72 Prometheus + Grafana (docker-compose)", font: "Arial", size: 22 })] }),
        new Paragraph({ numbering: { reference: "numbers", level: 0 }, spacing: { after: 80 },
          children: [new TextRun({ text: "\u532B\u5165 Grafana \u5100\u8868\u677F JSON", font: "Arial", size: 22 })] }),
        new Paragraph({ numbering: { reference: "numbers", level: 0 }, spacing: { after: 80 },
          children: [new TextRun({ text: "\u9A57\u8B49\u6307\u6A19\u53EF\u5B58\u53D6\u6027", font: "Arial", size: 22 })] }),
        heading("4.2 \u5E38\u898B\u554F\u984C\u6392\u9664", HeadingLevel.HEADING_2),
        new Table({
          width: { size: 9360, type: WidthType.DXA },
          columnWidths: [2000, 3000, 4360],
          rows: [
            new TableRow({ children: [headerCell("\u554F\u984C", 2000), headerCell("\u75C7\u72C0", 3000), headerCell("\u89E3\u6C7A\u65B9\u6CD5", 4360)] }),
            new TableRow({ children: [cell("Prometheus \u7121\u6CD5\u6293\u53D6", 2000), cell("Target \u72C0\u614B\u70BA DOWN", 3000), cell("\u78BA\u8A8D prometheus.yml \u4E2D\u7684 target \u4F4D\u5740\u8207 ERP \u5BE9\u96C4\u4F4D\u5740\u4E00\u81F4", 4360, altShading)] }),
            new TableRow({ children: [cell("Grafana \u7121\u8CC7\u6599", 2000), cell("\u5716\u8868\u986F\u793A No data", 3000), cell("\u78BA\u8A8D Prometheus \u5DF2\u6210\u529F\u6293\u53D6\u6307\u6A19 (\u67E5 Targets)", 4360)] }),
            new TableRow({ children: [cell("\u5EF6\u9072\u904E\u9AD8", 2000), cell("P95 > 500ms", 3000), cell("\u6AA2\u67E5\u8CC7\u6599\u5EAB\u67E5\u8A62\u6548\u80FD\uFF0C\u8003\u616E\u52A0\u5165\u5FEB\u53D6\u6A5F\u5236", 4360, altShading)] }),
          ]
        }),

        new Paragraph({ children: [new PageBreak()] }),
        // ── 5. Error Troubleshooting ──
        heading("5. \u5E38\u898B\u932F\u8AA4\u6392\u9664\u6E05\u55AE", HeadingLevel.HEADING_1),
        new Table({
          width: { size: 9360, type: WidthType.DXA },
          columnWidths: [1800, 1600, 2800, 3160],
          rows: [
            new TableRow({ children: [headerCell("\u932F\u8AA4\u985E\u578B", 1800), headerCell("\u75C7\u72C0", 1600), headerCell("\u6392\u9664\u6B65\u9A5F", 2800), headerCell("\u9810\u9632\u63AA\u65BD", 3160)] }),
            new TableRow({ children: [cell("\u8A8D\u8B49\u5931\u6548", 1800), cell("401/403 \u932F\u8AA4", 1600), cell("\u91CD\u8A2DKey\u3001\u6AA2\u67E5OAuth\u7BC4\u570D", 2800), cell("\u81EA\u52D5\u8F2A\u66FF\u3001\u74B0\u5883\u9694\u96E2", 3160, altShading)] }),
            new TableRow({ children: [cell("\u683C\u5F0F\u4E0D\u7B26", 1800), cell("400 \u89E3\u6790\u5931\u6548", 1600), cell("Schema \u9A57\u8B49\u3001\u65E5\u8A8C\u67E5\u8A62", 2800), cell("\u6A19\u6E96JSON\u3001\u7F3A\u503C\u898F\u5247", 3160)] }),
            new TableRow({ children: [cell("\u72C0\u614B\u4E0D\u540C\u6B65", 1800), cell("\u91CD\u8907\u55AE\u64DA", 1600), cell("\u8FFD\u8E64ID\u3001\u56DE\u653E\u4E8B\u4EF6", 2800), cell("Idempotency\u3001\u544A\u8B66", 3160, altShading)] }),
            new TableRow({ children: [cell("\u7DB2\u8DEF\u6545\u969C", 1800), cell("Timeout/\u65B7\u7DDA", 1600), cell("\u91CD\u8A66\u903F\u8F2F\u3001\u964D\u7D1A\u6A21\u5F0F", 2800), cell("\u76E3\u63A7\u5DE5\u5177\u3001\u591A\u7AEF\u9EDE\u5099\u63F4", 3160)] }),
          ]
        }),
      ]
    }
  ]
});

// ── Generate ──
Packer.toBuffer(doc).then(buffer => {
  fs.writeFileSync("D:/ERP/docs/ERP_AI_Agent_監控系統規格書.docx", buffer);
  console.log("Word document created successfully!");
});
