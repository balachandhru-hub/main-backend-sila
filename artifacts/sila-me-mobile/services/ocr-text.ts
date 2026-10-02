export function combinePageOcrText(pageTexts: Array<{ id: string; text: string }>) {
  return pageTexts.map((page, index) => `--- PAGE ${index + 1} ---\n${page.text}`).join('\n');
}