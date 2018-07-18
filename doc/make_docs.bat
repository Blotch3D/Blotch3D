pandoc -o readme.md Blotch3D.docx
copy readme.md ..
doxygen Blotch3D.doxy
cd latex
pdflatex refman.tex
pdflatex refman.tex
copy refman.pdf ..\Blotch3DManual.pdf
