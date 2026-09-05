# =====================================================================
# Сборка отчёта по лабораторной работе.
#   make lab            — скомпилировать PDF
#   make lab-clean      — удалить артефакты сборки
# =====================================================================

LAB_MAIN := lab/lab_report
LATEXMK  := latexmk -pdf -shell-escape -interaction=nonstopmode -halt-on-error

.PHONY: lab lab-clean

lab:
	$(LATEXMK) $(LAB_MAIN).tex

lab-clean:
	latexmk -C $(LAB_MAIN).tex
	rm -rf _minted-lab_report
