import { Injectable } from '@angular/core';
import { AdminDashboardDto } from '../../../core/models/models';
import jsPDF from 'jspdf';

@Injectable({ providedIn: 'root' })
export class ReportService {

  downloadReport(d: AdminDashboardDto): void {
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    const W = 210, margin = 16, contentW = W - margin * 2;
    let y = 0;

    // ── helpers ──────────────────────────────────────────────────────────────
    const hex = (h: string) => {
      const r = parseInt(h.slice(1, 3), 16);
      const g = parseInt(h.slice(3, 5), 16);
      const b = parseInt(h.slice(5, 7), 16);
      return [r, g, b] as [number, number, number];
    };
    const setColor = (h: string) => doc.setTextColor(...hex(h));
    const setFill  = (h: string) => doc.setFillColor(...hex(h));
    const setDraw  = (h: string) => doc.setDrawColor(...hex(h));

    // ── header band ───────────────────────────────────────────────────────────
    setFill('#2d3a8c');
    doc.rect(0, 0, W, 28, 'F');
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(18);
    doc.setTextColor(255, 255, 255);
    doc.text(d.hotelName, margin, 13);
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.text('Hotel Analysis Report  ·  Generated ' + new Date().toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }), margin, 21);
    y = 36;

    // ── KPI row ───────────────────────────────────────────────────────────────
    const kpis = [
      { label: 'Total Rooms',     value: `${d.activeRooms}/${d.totalRooms}`, color: '#2d3a8c' },
      { label: 'Reservations',    value: String(d.totalReservations),         color: '#2e7d32' },
      { label: 'Total Revenue',   value: '₹' + d.totalRevenue.toLocaleString('en-IN'), color: '#c97a1b' },
      { label: 'Avg Rating',      value: d.averageRating.toFixed(1) + ' ★',  color: '#f59e0b' },
    ];
    const kpiW = contentW / 4 - 2;
    kpis.forEach((k, i) => {
      const x = margin + i * (kpiW + 2.7);
      setFill('#f5f7ff');
      setDraw('#e0e4f5');
      doc.roundedRect(x, y, kpiW, 18, 2, 2, 'FD');
      doc.setFont('helvetica', 'bold');
      doc.setFontSize(13);
      setColor(k.color);
      doc.text(k.value, x + kpiW / 2, y + 9, { align: 'center' });
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(7);
      setColor('#666666');
      doc.text(k.label.toUpperCase(), x + kpiW / 2, y + 14.5, { align: 'center' });
    });
    y += 26;

    // ── reservation status bar chart (canvas → image) ─────────────────────────
    const canvas = document.createElement('canvas');
    canvas.width = 520; canvas.height = 180;
    const ctx = canvas.getContext('2d')!;

    const statuses = [
      { label: 'Pending',   value: d.pendingReservations,   color: '#f59e0b' },
      { label: 'Confirmed', value: d.activeReservations,    color: '#2e7d32' },
      { label: 'Completed', value: d.completedReservations, color: '#2d3a8c' },
      { label: 'Cancelled', value: d.cancelledReservations, color: '#c62828' },
    ];

    const maxVal = Math.max(...statuses.map(s => s.value), 1);
    const barH = 28, gap = 12, startX = 90, chartW = 400;

    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    statuses.forEach((s, i) => {
      const bY = 10 + i * (barH + gap);
      const bW = Math.max((s.value / maxVal) * chartW, s.value > 0 ? 4 : 0);

      // label
      ctx.fillStyle = '#444';
      ctx.font = '13px Arial';
      ctx.textAlign = 'right';
      ctx.fillText(s.label, 82, bY + barH / 2 + 5);

      // bar
      ctx.fillStyle = s.color;
      ctx.beginPath();
      ctx.roundRect(startX, bY, bW, barH, 4);
      ctx.fill();

      // value
      ctx.fillStyle = '#333';
      ctx.font = 'bold 13px Arial';
      ctx.textAlign = 'left';
      ctx.fillText(String(s.value), startX + bW + 6, bY + barH / 2 + 5);
    });

    const barImg = canvas.toDataURL('image/png');

    // section title
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10);
    setColor('#2d3a8c');
    doc.text('RESERVATION BREAKDOWN', margin, y);
    y += 4;
    doc.addImage(barImg, 'PNG', margin, y, contentW, 36);
    y += 42;

    // ── donut chart for reservation split ─────────────────────────────────────
    const donutCanvas = document.createElement('canvas');
    donutCanvas.width = 200; donutCanvas.height = 200;
    const dc = donutCanvas.getContext('2d')!;

    const total = d.totalReservations || 1;
    const slices = statuses.filter(s => s.value > 0);
    let startAngle = -Math.PI / 2;
    const cx = 100, cy = 100, outerR = 80, innerR = 48;

    slices.forEach(s => {
      const angle = (s.value / total) * 2 * Math.PI;
      dc.beginPath();
      dc.moveTo(cx, cy);
      dc.arc(cx, cy, outerR, startAngle, startAngle + angle);
      dc.closePath();
      dc.fillStyle = s.color;
      dc.fill();
      startAngle += angle;
    });

    // inner white circle (donut hole)
    dc.beginPath();
    dc.arc(cx, cy, innerR, 0, 2 * Math.PI);
    dc.fillStyle = '#ffffff';
    dc.fill();

    // center text
    dc.fillStyle = '#333';
    dc.font = 'bold 18px Arial';
    dc.textAlign = 'center';
    dc.fillText(String(d.totalReservations), cx, cy + 4);
    dc.font = '11px Arial';
    dc.fillStyle = '#888';
    dc.fillText('Total', cx, cy + 18);

    const donutImg = donutCanvas.toDataURL('image/png');
    doc.addImage(donutImg, 'PNG', margin, y, 42, 42);

    // legend next to donut
    let ly = y + 4;
    slices.forEach(s => {
      setFill(s.color);
      doc.rect(margin + 46, ly, 4, 4, 'F');
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(8);
      setColor('#333333');
      doc.text(`${s.label}: ${s.value} (${((s.value / total) * 100).toFixed(1)}%)`, margin + 53, ly + 3.5);
      ly += 8;
    });
    y += 48;

    // ── revenue & reviews summary ─────────────────────────────────────────────
    y += 4;
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10);
    setColor('#2d3a8c');
    doc.text('FINANCIAL & REVIEW SUMMARY', margin, y);
    y += 6;

    const summaryRows = [
      ['Total Revenue',    '₹' + d.totalRevenue.toLocaleString('en-IN')],
      ['Total Reviews',    String(d.totalReviews)],
      ['Average Rating',   d.averageRating.toFixed(2) + ' / 5.0'],
      ['Active Rooms',     `${d.activeRooms} of ${d.totalRooms}`],
      ['Room Types',       String(d.totalRoomTypes)],
    ];

    summaryRows.forEach(([label, val], i) => {
      const rowY = y + i * 8;
      setFill(i % 2 === 0 ? '#f5f7ff' : '#ffffff');
      doc.rect(margin, rowY, contentW, 7.5, 'F');
      doc.setFont('helvetica', 'normal');
      doc.setFontSize(9);
      setColor('#444444');
      doc.text(label, margin + 3, rowY + 5);
      doc.setFont('helvetica', 'bold');
      setColor('#2d3a8c');
      doc.text(val, margin + contentW - 3, rowY + 5, { align: 'right' });
    });
    y += summaryRows.length * 8 + 6;

    // ── rating bar ────────────────────────────────────────────────────────────
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10);
    setColor('#2d3a8c');
    doc.text('RATING SCORE', margin, y);
    y += 5;

    const ratingPct = (d.averageRating / 5) * 100;
    setFill('#e0e4f5');
    doc.roundedRect(margin, y, contentW, 7, 3, 3, 'F');
    setFill('#f59e0b');
    doc.roundedRect(margin, y, contentW * ratingPct / 100, 7, 3, 3, 'F');
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(8);
    setColor('#ffffff');
    doc.text(`${d.averageRating.toFixed(1)} / 5.0`, margin + 4, y + 5);
    y += 14;

    // ── footer ────────────────────────────────────────────────────────────────
    const pageH = 297;
    setFill('#2d3a8c');
    doc.rect(0, pageH - 12, W, 12, 'F');
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(7);
    doc.setTextColor(255, 255, 255);
    doc.text(`${d.hotelName}  ·  Confidential Hotel Report`, margin, pageH - 5);
    doc.text('Page 1 of 1', W - margin, pageH - 5, { align: 'right' });

    doc.save(`${d.hotelName.replace(/\s+/g, '_')}_Report_${new Date().toISOString().slice(0, 10)}.pdf`);
  }
}
