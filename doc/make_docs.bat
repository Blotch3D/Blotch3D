pandoc Blotch3DUserManual.docx -o ../readme.md
pandoc Blotch3DUserManual.docx -o Blotch3DUserManual.pdf
doxygen Blotch3D.doxy
cd latex
pdflatex refman.tex
pdflatex refman.tex
cd ..
copy latex\refman.pdf Blotch3D_Reference_Manual.pdf
pause