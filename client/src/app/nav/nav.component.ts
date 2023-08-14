import { Component, OnInit } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { Observable, of } from 'rxjs';
import { User } from '../_models/user';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent implements OnInit {
  model: any = {};

  constructor(public accountServices: AccountService, private router: Router, 
    private toastr: ToastrService) { }

  ngOnInit(): void {
  }

  // getCurrentUser() {
  //   this.accountSevices.currentUser$.subscribe({
  //     next: user => this.isLoggedIn = !!user,
  //     error: error => console.log(error)
      
  //   })
  // }

  login() {
    this.accountServices.login(this.model).subscribe({
      next: respones => {
        console.log(respones)
        this.router.navigateByUrl('/members')
        this.model = {}
      },
      // error: error => {
      //   console.log(error)
      //   this.toastr.error(error.error)
      // }     
    })
  }

  logout() {
    this.accountServices.logout();
    this.router.navigateByUrl('/')
  }

}
